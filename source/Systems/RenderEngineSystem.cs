using Collections.Generic;
using Materials.Components;
using Meshes.Components;
using Rendering.Components;
using Rendering.Functions;
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
    public class RenderEngineSystem : ISystem, IDisposable
    {
        private readonly List<Destination> knownDestinations;
        private readonly List<Destination> destinationsWithSurfaces;
        private readonly System.Collections.Generic.Dictionary<RendererLabel, RenderingBackend> availableBackends;
        private readonly System.Collections.Generic.Dictionary<Destination, RenderingMachine> renderingMachines;
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

        public RenderEngineSystem(Simulator simulator)
        {
            knownDestinations = new(4);
            destinationsWithSurfaces = new(4);
            availableBackends = new(4);
            renderingMachines = new(4);
            entityComponents = new(4);
            pluginFunctions = new(4);

            Schema schema = simulator.world.Schema;
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

        public void Dispose()
        {
            pluginFunctions.Dispose();
            entityComponents.Dispose();

            foreach (RenderingMachine renderSystem in renderingMachines.Values)
            {
                foreach (ViewportGroup group in renderSystem.viewportGroups.Values)
                {
                    group.Dispose();
                }

                renderSystem.viewportGroups.Dispose();
                renderSystem.Dispose();
            }

            foreach (RenderingBackend rendererBackend in availableBackends.Values)
            {
                rendererBackend.Dispose();
            }

            knownDestinations.Dispose();
            destinationsWithSurfaces.Dispose();
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            World world = simulator.world;
            Schema schema = world.Schema;
            CollectPlugins(world, pluginType);
            DestroyOldSystems(world);
            CreateRenderMachines(world, destinationType, rendererInstanceInUseType, destinationExtensionType);
            AssignSurfaces(world, surfaceInUseType);
            CollectComponents(world, rendererType, materialType, shaderType, meshType);
            CollectRenderers(world, viewportType);
            Render(world, destinationType);
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

        public T UnregisterRenderingBackend<T>(bool dispose = true) where T : RenderingBackend
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

            if (label == default)
            {
                throw new InvalidOperationException($"Rendering backend of type {typeof(T)} not found to unregister");
            }

            availableBackends.Remove(label, out RenderingBackend? renderingBackend);
            if (dispose)
            {
                renderingBackend!.Dispose();
            }

            return (T)renderingBackend!;
        }

        private void CreateRenderMachines(World world, int destinationType, int rendererInstanceType, int destinationExtensionType)
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
                        ref IsDestination component = ref components[i];
                        Destination destination = Entity.Get<Destination>(world, entities[i]);
                        if (knownDestinations.Contains(destination))
                        {
                            return;
                        }

                        CreateRenderMachineForDestination(destination, component.rendererLabel, rendererInstanceType, destinationExtensionType);
                    }
                }
            }
        }

        private void CreateRenderMachineForDestination(Destination destination, RendererLabel label, int rendererInstanceType, int destinationExtensionType)
        {
            if (availableBackends.TryGetValue(label, out RenderingBackend? renderingBackend))
            {
                RenderingMachine renderingMachine = renderingBackend.CreateRenderingMachine(destination);
                renderingMachines.Add(destination, renderingMachine);
                knownDestinations.Add(destination);
                destination.AddComponent(rendererInstanceType, new RendererInstanceInUse(renderingMachine.Instance));
                Trace.WriteLine($"Created render system for destination `{destination}` with label `{label}`");
            }
            else
            {
                throw new InvalidOperationException($"Unknown renderer label `{label}`, no rendering backends available to handle it");
            }
        }

        private void CollectComponents(World world, int rendererType, int materialType, int shaderType, int meshType)
        {
            int capacity = (world.MaxEntityValue + 1).GetNextPowerOf2();
            if (this.entityComponents.Length < capacity)
            {
                this.entityComponents.Length = capacity;
            }

            Span<EntityComponents> entityComponents = this.entityComponents.AsSpan();
            entityComponents.Clear();

            //collect all components
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                BitMask componentTypes = definition.componentTypes;
                ReadOnlySpan<uint> entities = chunk.Entities;
                if (componentTypes.Contains(rendererType) && !definition.ContainsTag(Schema.DisabledTagType))
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

        private void CollectRenderers(World world, int viewportType)
        {
            //collect renderers and store them with the viewports that they render to
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(viewportType) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> viewportEntities = chunk.Entities;
                    ComponentEnumerator<IsViewport> viewportComponents = chunk.GetComponents<IsViewport>(viewportType);
                    for (int i = 0; i < viewportEntities.Length; i++)
                    {
                        uint viewportEntity = viewportEntities[i];
                        IsViewport viewport = viewportComponents[i];
                        uint destinationEntity = world.GetReference(viewportEntity, viewport.destinationReference);
                        if (!world.ContainsEntity(destinationEntity))
                        {
                            continue;
                        }

                        Destination destination = Entity.Get<Destination>(world, destinationEntity);
                        if (renderingMachines.TryGetValue(destination, out RenderingMachine? renderingMachine))
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
                            for (uint entity = 1; entity < entityComponents.Length; entity++)
                            {
                                EntityComponents rendererComponents = entityComponents[(int)entity];
                                if (rendererComponents.ContainsRenderer)
                                {
                                    IsRenderer renderer = rendererComponents.renderer;
                                    if (renderer.renderMask.ContainsAny(viewportMask))
                                    {
                                        if (renderer.materialReference != default && renderer.meshReference != default)
                                        {
                                            uint materialEntity = world.GetReference(entity, renderer.materialReference);
                                            EntityComponents materialComponents = entityComponents[(int)materialEntity];
                                            if (!materialComponents.ContainsMaterial)
                                            {
                                                //material entity is missing the component
                                                continue;
                                            }

                                            uint meshEntity = world.GetReference(entity, renderer.meshReference);
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
                                                RenderEntity renderEntity = new(entity, meshEntity, materialEntity, vertexShaderEntity, fragmentShaderEntity, mesh.version, vertexShader.version, fragmentShader.version);
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

        private void CollectPlugins(World world, int pluginType)
        {
            pluginFunctions.Clear();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(pluginType))
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

        private void AssignSurfaces(World world, int surfaceInUseType)
        {
            foreach (Destination destination in knownDestinations)
            {
                if (destination.world != world)
                {
                    continue;
                }

                //notify the entity that surface has been created
                if (!destinationsWithSurfaces.Contains(destination))
                {
                    if (destination.TryGetComponent(surfaceInUseType, out SurfaceInUse surfaceInUse))
                    {
                        destinationsWithSurfaces.Add(destination);
                        RenderingMachine renderingMachine = renderingMachines[destination];
                        renderingMachine.SurfaceCreated(surfaceInUse.value);
                    }
                }
            }
        }

        private void Render(World world, int destinationType)
        {
            ReadOnlySpan<Destination> knownDestinations = this.knownDestinations.AsSpan();
            foreach (Destination destination in knownDestinations)
            {
                if (destination.world == world)
                {
                    ref IsDestination component = ref destination.GetComponent<IsDestination>(destinationType);
                    if (component.Area == 0)
                    {
                        continue; //no area to render to
                    }

                    if (destinationsWithSurfaces.Contains(destination))
                    {
                        RenderingMachine renderingMachine = renderingMachines[destination];
                        if (renderingMachine.BeginRender(component.clearColor))
                        {
                            Render(world, renderingMachine);
                        }
                        else
                        {
                            //skipped rendering
                        }
                    }
                }
            }
        }

        private void Render(World world, RenderingMachine renderingMachine)
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
                Render(world, renderingMachine, viewportEntity, viewportGroup);
            }

            renderingMachine.EndRender();
        }

        private void Render(World world, RenderingMachine renderingMachine, uint viewportEntity, ViewportGroup viewportGroup)
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
            ReadOnlySpan<RenderEnginePluginFunction> pluginFunctions = this.pluginFunctions.AsSpan();
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

        private void DestroyOldSystems(World world)
        {
            for (int i = knownDestinations.Count - 1; i >= 0; i--)
            {
                Destination destination = knownDestinations[i];
                if (destination.world != world)
                {
                    continue;
                }

                if (destination.IsDestroyed)
                {
                    renderingMachines.Remove(destination, out RenderingMachine? renderingMachine);
                    renderingMachine!.Dispose();
                    knownDestinations.RemoveAt(i);
                    Trace.WriteLine($"Removed render system for destination `{destination}`");
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