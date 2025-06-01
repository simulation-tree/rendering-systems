using Collections.Generic;
using Materials.Components;
using Meshes.Components;
using Rendering.Components;
using Rendering.Functions;
using Rendering.Messages;
using Shaders.Components;
using Simulation;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;
using Worlds;

namespace Rendering.Systems
{
    [SkipLocalsInit]
    public partial class RenderEngineSystem : SystemBase, IListener<RenderUpdate>
    {
        private readonly World world;
        private readonly List<uint> knownDestinations;
        private readonly List<uint> destinationsWithSurfaces;
        private readonly System.Collections.Generic.Dictionary<RendererLabel, RenderingBackend> availableBackends;
        private readonly System.Collections.Generic.Dictionary<uint, RenderingMachine> renderingMachines;
        private readonly Array<EntityComponents> entityComponents;
        private readonly List<RenderEnginePluginFunction> pluginFunctions;
        private readonly int destinationType;
        private readonly int rendererType;
        private readonly int viewportType;
        private readonly int materialType;
        private readonly int shaderType;
        private readonly int meshType;
        private readonly int surfaceInUseType;
        private readonly int rendererInstanceInUseType;
        private readonly int destinationExtensionType;
        private readonly int pluginType;

        public RenderEngineSystem(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            knownDestinations = new(4);
            destinationsWithSurfaces = new(4);
            availableBackends = new(4);
            renderingMachines = new(4);
            entityComponents = new(4);
            pluginFunctions = new(4);

            Schema schema = world.Schema;
            destinationType = schema.GetComponentType<IsDestination>();
            rendererType = schema.GetComponentType<IsRenderer>();
            viewportType = schema.GetComponentType<IsViewport>();
            materialType = schema.GetComponentType<IsMaterial>();
            shaderType = schema.GetComponentType<IsShader>();
            meshType = schema.GetComponentType<IsMesh>();
            surfaceInUseType = schema.GetComponentType<SurfaceInUse>();
            rendererInstanceInUseType = schema.GetComponentType<RendererInstanceInUse>();
            destinationExtensionType = schema.GetArrayType<DestinationExtension>();
            pluginType = schema.GetComponentType<RenderEnginePluginFunction>();
        }

        public override void Dispose()
        {
            pluginFunctions.Dispose();
            entityComponents.Dispose();

            foreach (RenderingBackend rendererBackend in availableBackends.Values)
            {
                for (int m = rendererBackend.renderingMachines.Count - 1; m >= 0; m--)
                {
                    RenderingMachine renderingMachine = rendererBackend.renderingMachines[m];
                    rendererBackend.DisposeRenderingMachine(renderingMachine);
                    foreach (ViewportGroup viewportGroup in renderingMachine.viewportGroups.Values)
                    {
                        viewportGroup.Dispose();
                    }

                    renderingMachine.viewportGroups.Dispose();
                }

                rendererBackend.renderingMachines.Clear();
                rendererBackend.Dispose();
            }

            availableBackends.Clear();
            renderingMachines.Clear();
            knownDestinations.Dispose();
            destinationsWithSurfaces.Dispose();
        }

        void IListener<RenderUpdate>.Receive(ref RenderUpdate message)
        {
            CollectPlugins();
            DestroyOldSystems();
            CreateRenderMachines();
            AssignSurfaces();
            CollectComponents();
            CollectRenderers();
            Render();
        }

        /// <summary>
        /// Makes the given render system type available for use at runtime,
        /// for destinations that reference its label.
        /// </summary>
        public void RegisterRenderingBackend<T>(T renderingBackend) where T : RenderingBackend
        {
            RendererLabel label = new(renderingBackend.Label);
            if (availableBackends.ContainsKey(label))
            {
                throw new InvalidOperationException($"Label `{renderingBackend.Label.ToString()}` already has a render system registered for");
            }

            availableBackends.Add(label, renderingBackend);
        }

        public void UnregisterRenderingBackend<T>() where T : RenderingBackend
        {
            RendererLabel label = default;
            foreach (System.Collections.Generic.KeyValuePair<RendererLabel, RenderingBackend> pair in availableBackends)
            {
                if (pair.Value is T backend)
                {
                    label = pair.Key;
                    break;
                }
            }

            if (availableBackends.Remove(label, out RenderingBackend? renderingBackend))
            {
                for (int i = renderingBackend.renderingMachines.Count - 1; i >= 0; i--)
                {
                    RenderingMachine renderingMachine = renderingBackend.renderingMachines[i];
                    renderingBackend.DisposeRenderingMachine(renderingMachine);
                    foreach (ViewportGroup viewportGroup in renderingMachine.viewportGroups.Values)
                    {
                        viewportGroup.Dispose();
                    }

                    renderingMachine.viewportGroups.Dispose();
                    renderingMachines.Remove(renderingMachine.destination.GetEntityValue());
                }

                renderingBackend.renderingMachines.Clear();
                renderingBackend.Dispose();
            }
            else
            {
                throw new InvalidOperationException($"Rendering backend of type {typeof(T)} not found to unregister");
            }
        }

        private void CreateRenderMachines()
        {
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(destinationType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsDestination> components = chunk.GetComponents<IsDestination>(destinationType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsDestination destination = ref components[i];
                        uint destinationEntity = entities[i];
                        if (knownDestinations.Contains(destinationEntity))
                        {
                            return;
                        }

                        CreateRenderMachineForDestination(destinationEntity, destination);
                    }
                }
            }
        }

        private void CreateRenderMachineForDestination(uint destinationEntity, IsDestination destination)
        {
            if (availableBackends.TryGetValue(destination.rendererLabel, out RenderingBackend? renderingBackend))
            {
                RenderingMachine renderingMachine = renderingBackend.CreateRenderingMachine(Entity.Get<Destination>(world, destinationEntity));
                renderingMachine.renderingBackend = renderingBackend;
                renderingMachines.Add(destinationEntity, renderingMachine);
                knownDestinations.Add(destinationEntity);
                renderingBackend.renderingMachines.Add(renderingMachine);
                world.AddComponent(destinationEntity, rendererInstanceInUseType, new RendererInstanceInUse(renderingMachine.instance));
                Trace.WriteLine($"Created render system for destination `{destinationEntity}` with label `{destination.rendererLabel}`");
            }
            else
            {
                throw new InvalidOperationException($"Unknown renderer label `{destination.rendererLabel}`, no rendering backends available to handle it");
            }
        }

        private void CollectComponents()
        {
            int capacity = (world.MaxEntityValue + 1).GetNextPowerOf2();
            if (this.entityComponents.Length < capacity)
            {
                this.entityComponents.Length = capacity;
            }

            Span<EntityComponents> entityComponents = this.entityComponents.AsSpan();
            entityComponents.Clear();

            //collect all components
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                Definition definition = chunk.Definition;
                BitMask componentTypes = definition.componentTypes;
                ReadOnlySpan<uint> entities = chunk.Entities;
                if (componentTypes.Contains(rendererType) && !definition.IsDisabled)
                {
                    ComponentEnumerator<IsRenderer> components = chunk.GetComponents<IsRenderer>(rendererType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        entityComponents[(int)entities[i]].SetRenderer(components[i]);
                    }
                }

                if (componentTypes.Contains(materialType))
                {
                    ComponentEnumerator<IsMaterial> components = chunk.GetComponents<IsMaterial>(materialType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        entityComponents[(int)entities[i]].SetMaterial(components[i]);
                    }
                }

                if (componentTypes.Contains(shaderType))
                {
                    ComponentEnumerator<IsShader> components = chunk.GetComponents<IsShader>(shaderType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        entityComponents[(int)entities[i]].SetShader(components[i]);
                    }
                }

                if (componentTypes.Contains(meshType))
                {
                    ComponentEnumerator<IsMesh> components = chunk.GetComponents<IsMesh>(meshType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        entityComponents[(int)entities[i]].SetMesh(components[i]);
                    }
                }
            }
        }

        private void CollectRenderers()
        {
            //collect renderers and store them with the viewports that they render to
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(viewportType) && !definition.IsDisabled)
                {
                    ReadOnlySpan<uint> viewportEntities = chunk.Entities;
                    ComponentEnumerator<IsViewport> viewportComponents = chunk.GetComponents<IsViewport>(viewportType);
                    for (int v = 0; v < viewportEntities.Length; v++)
                    {
                        uint viewportEntity = viewportEntities[v];
                        IsViewport viewport = viewportComponents[v];
                        uint destinationEntity = world.GetReference(viewportEntity, viewport.destinationReference);
                        if (!world.ContainsEntity(destinationEntity))
                        {
                            continue;
                        }

                        //Destination destination = Entity.Get<Destination>(world, destinationEntity);
                        if (renderingMachines.TryGetValue(destinationEntity, out RenderingMachine? renderingMachine))
                        {
                            ref ViewportGroup viewportGroup = ref renderingMachine.viewportGroups.TryGetValue(viewportEntity, out bool containsViewportGroup);
                            if (!containsViewportGroup)
                            {
                                viewportGroup = ref renderingMachine.viewportGroups.Add(viewportEntity);
                                viewportGroup = new();
                            }
                            else
                            {
                                viewportGroup.Reset();
                            }

                            viewportGroup.order = viewport.order;
                            LayerMask viewportMask = viewport.renderMask;
                            for (uint e = 1; e < entityComponents.Length; e++)
                            {
                                EntityComponents rendererComponents = entityComponents[(int)e];
                                if (rendererComponents.ContainsRenderer)
                                {
                                    IsRenderer renderer = rendererComponents.renderer;
                                    if (renderer.renderMask.ContainsAny(viewportMask))
                                    {
                                        if (renderer.materialReference != default && renderer.meshReference != default)
                                        {
                                            uint materialEntity = world.GetReference(e, renderer.materialReference);
                                            EntityComponents materialComponents = entityComponents[(int)materialEntity];
                                            if (!materialComponents.ContainsMaterial)
                                            {
                                                //material entity is missing the component
                                                continue;
                                            }

                                            uint meshEntity = world.GetReference(e, renderer.meshReference);
                                            EntityComponents meshComponents = entityComponents[(int)meshEntity];
                                            if (!meshComponents.ContainsMesh)
                                            {
                                                //mesh entity is missing the component
                                                continue;
                                            }

                                            IsMesh mesh = meshComponents.mesh;
                                            IsMaterial material = materialComponents.material;
                                            if (material.vertexShaderReference != default && material.fragmentShaderReference != default)
                                            {
                                                uint vertexShaderEntity = world.GetReference(materialEntity, material.vertexShaderReference);
                                                EntityComponents vertexShaderComponents = entityComponents[(int)vertexShaderEntity];
                                                if (!vertexShaderComponents.ContainsShader)
                                                {
                                                    //vertex shader entity is missing the component
                                                    continue;
                                                }

                                                uint fragmentShaderEntity = world.GetReference(materialEntity, material.fragmentShaderReference);
                                                EntityComponents fragmentShaderComponents = entityComponents[(int)fragmentShaderEntity];
                                                if (!fragmentShaderComponents.ContainsShader)
                                                {
                                                    //fragment shader entity is missing the component
                                                    continue;
                                                }

                                                //todo: for efficiency, the work of filtering out valid renderer entities could be done once
                                                //instead of per viewport
                                                IsShader vertexShader = entityComponents[(int)vertexShaderEntity].shader;
                                                IsShader fragmentShader = entityComponents[(int)fragmentShaderEntity].shader;
                                                RenderEntity renderEntity = new(e, meshEntity, materialEntity, vertexShaderEntity, fragmentShaderEntity, mesh.version, vertexShader.version, fragmentShader.version);
                                                ref List<RenderEntity> renderEntities = ref viewportGroup.map.TryGetValue(material.renderGroup, out bool containsMaterialGroup);
                                                if (!containsMaterialGroup)
                                                {
                                                    renderEntities = ref viewportGroup.map.Add(material.renderGroup);
                                                    renderEntities = new(256);
                                                }

                                                renderEntities.Add(renderEntity);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //system with label not found
                        }
                    }
                }
            }
        }

        private void CollectPlugins()
        {
            pluginFunctions.Clear();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.Definition.ContainsComponent(pluginType))
                {
                    int count = chunk.Count;
                    ComponentEnumerator<RenderEnginePluginFunction> components = chunk.GetComponents<RenderEnginePluginFunction>(pluginType);
                    for (int i = 0; i < count; i++)
                    {
                        pluginFunctions.Add(components[i]);
                    }
                }
            }
        }

        private void AssignSurfaces()
        {
            Span<uint> destinations = knownDestinations.AsSpan();
            for (int d = 0; d < destinations.Length; d++)
            {
                //notify the entity that surface has been created
                uint destinationEntity = destinations[d];
                if (!destinationsWithSurfaces.Contains(destinationEntity))
                {
                    if (world.TryGetComponent(destinationEntity, surfaceInUseType, out SurfaceInUse surfaceInUse))
                    {
                        destinationsWithSurfaces.Add(destinationEntity);
                        renderingMachines[destinationEntity].SurfaceCreated(surfaceInUse.value);
                    }
                }
            }
        }

        private void Render()
        {
            Span<uint> destinations = knownDestinations.AsSpan();
            for (int d = 0; d < destinations.Length; d++)
            {
                uint destinationEntity = destinations[d];
                ref IsDestination destination = ref world.GetComponent<IsDestination>(destinationEntity, destinationType);
                if (destination.width * destination.height == 0)
                {
                    continue; //no area to render to
                }

                if (destinationsWithSurfaces.Contains(destinationEntity))
                {
                    RenderingMachine renderingMachine = renderingMachines[destinationEntity];
                    if (renderingMachine.BeginRender(destination.clearColor))
                    {
                        Render(renderingMachine);
                    }
                    else
                    {
                        //skipped rendering
                    }
                }
            }
        }

        private void Render(RenderingMachine renderingMachine)
        {
            //sort viewports by their declared order
            Span<(uint viewportEntity, ViewportGroup viewportGroup)> viewportGroups = stackalloc (uint, ViewportGroup)[renderingMachine.viewportGroups.Count];
            int viewportCount = 0;
            foreach ((uint viewportEntity, ViewportGroup viewportGroup) in renderingMachine.viewportGroups)
            {
                viewportGroups[viewportCount++] = (viewportEntity, viewportGroup);
            }

            viewportGroups.Sort(SortByViewportOrder);

            //render all viewports
            for (int v = 0; v < viewportCount; v++)
            {
                (uint viewportEntity, ViewportGroup viewportGroup) = viewportGroups[v];
                Render(renderingMachine, viewportEntity, viewportGroup);
            }

            renderingMachine.EndRender();
        }

        private void Render(RenderingMachine renderingMachine, uint viewportEntity, ViewportGroup viewportGroup)
        {
            //sort material groups by their declared order
            Span<(sbyte renderGroup, List<RenderEntity> renderEntities)> materialGroups = stackalloc (sbyte, List<RenderEntity>)[viewportGroup.map.Count];
            int materialCount = 0;
            foreach ((sbyte renderGroup, List<RenderEntity> renderEntities) in viewportGroup.map)
            {
                materialGroups[materialCount++] = (renderGroup, renderEntities);
            }

            materialGroups.Sort(SortByMaterialOrder);

            //render all materials
            Span<RenderEnginePluginFunction> pluginFunctions = this.pluginFunctions.AsSpan();
            for (int m = 0; m < materialCount; m++)
            {
                //preprocess
                (sbyte renderGroup, List<RenderEntity> renderEntities) = materialGroups[m];
                Span<RenderEntity> entities = renderEntities.AsSpan();
                for (int p = 0; p < pluginFunctions.Length; p++)
                {
                    pluginFunctions[p].Invoke(world, renderGroup, entities);
                }

                renderingMachine.Render(renderGroup, entities);
            }
        }

        private void DestroyOldSystems()
        {
            for (int i = knownDestinations.Count - 1; i >= 0; i--)
            {
                uint destinationEntity = knownDestinations[i];
                if (!world.ContainsEntity(destinationEntity) && renderingMachines.Remove(destinationEntity, out RenderingMachine? renderingMachine))
                {
                    renderingMachine.renderingBackend?.DisposeRenderingMachine(renderingMachine);
                    foreach (ViewportGroup viewportGroup in renderingMachine.viewportGroups.Values)
                    {
                        viewportGroup.Dispose();
                    }

                    renderingMachine.viewportGroups.Dispose();
                    knownDestinations.RemoveAt(i);
                    Trace.WriteLine($"Removed render system for destination entity `{destinationEntity}`");
                }
            }
        }

        private static int SortByViewportOrder((uint, ViewportGroup group) x, (uint, ViewportGroup group) y)
        {
            return x.group.order.CompareTo(y.group.order);
        }

        private static int SortByMaterialOrder((sbyte renderGroup, List<RenderEntity>) x, (sbyte renderGroup, List<RenderEntity>) y)
        {
            return x.renderGroup.CompareTo(y.renderGroup);
        }
    }
}