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
    public readonly partial struct RenderEngineSystem : ISystem
    {
        private readonly List<Destination> knownDestinations;
        private readonly Dictionary<long, RenderingBackend> availableBackends;
        private readonly Dictionary<Destination, RenderingMachine> renderingMachines;
        private readonly Array<EntityComponents> entityComponents;
        private readonly List<RenderEnginePluginFunction> pluginFunctions;

        public RenderEngineSystem()
        {
            knownDestinations = new();
            availableBackends = new();
            renderingMachines = new();
            entityComponents = new();
            pluginFunctions = new();
        }

        public readonly void Dispose()
        {
            pluginFunctions.Dispose();
            entityComponents.Dispose();

            foreach (RenderingMachine renderSystem in renderingMachines.Values)
            {
                renderSystem.Dispose();
            }

            renderingMachines.Dispose();
            knownDestinations.Dispose();

            foreach (RenderingBackend rendererBackend in availableBackends.Values)
            {
                rendererBackend.Dispose();
            }

            availableBackends.Dispose();
        }

        readonly void ISystem.Start(in SystemContext context, in World world)
        {
        }

        readonly void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            Schema schema = world.Schema;
            int destinationType = schema.GetComponentType<IsDestination>();
            int rendererType = schema.GetComponentType<IsRenderer>();
            int viewportType = schema.GetComponentType<IsViewport>();
            int materialType = schema.GetComponentType<IsMaterial>();
            int shaderType = schema.GetComponentType<IsShader>();
            int meshType = schema.GetComponentType<IsMesh>();
            int surfaceInUseType = schema.GetComponentType<SurfaceInUse>();
            int rendererInstanceInUseType = schema.GetComponentType<RendererInstanceInUse>();
            int destinationExtensionType = schema.GetArrayType<DestinationExtension>();
            int pluginType = schema.GetComponentType<RenderEnginePluginFunction>();
            if (context.IsSimulatorWorld(world))
            {
                CollectPlugins(world, pluginType);
            }

            DestroyOldSystems(world);
            CreateRenderMachines(world, destinationType, rendererInstanceInUseType, destinationExtensionType);
            CreateSurfaces(world, surfaceInUseType);
            CollectComponents(world, rendererType, materialType, shaderType, meshType);
            CollectRenderers(world, viewportType);
            Render(world, destinationType, surfaceInUseType);
        }

        readonly void ISystem.Finish(in SystemContext context, in World world)
        {
        }

        /// <summary>
        /// Makes the given render system type available for use at runtime,
        /// for destinations that reference its label.
        /// </summary>
        public readonly void RegisterRenderingBackend<T>() where T : unmanaged, IRenderingBackend
        {
            ASCIIText256 label = default(T).Label;
            long hash = label.GetLongHashCode();
            if (availableBackends.ContainsKey(hash))
            {
                throw new InvalidOperationException($"Label `{label}` already has a render system registered for");
            }

            RenderingBackend systemCreator = RenderingBackend.Create<T>();
            availableBackends.Add(hash, systemCreator);
        }

        private readonly void CreateRenderMachines(World world, int destinationType, int rendererInstanceType, int destinationExtensionType)
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
                        Destination entity = Entity.Get<Destination>(world, entities[i]);
                        if (knownDestinations.Contains(entity))
                        {
                            return;
                        }

                        CreateRenderMachineForDestination(entity, component, rendererInstanceType, destinationExtensionType);
                    }
                }
            }
        }

        private readonly void CreateRenderMachineForDestination(Destination entity, IsDestination destination, int rendererInstanceType, int destinationExtensionType)
        {
            ASCIIText256 label = destination.rendererLabel;
            long hash = label.GetLongHashCode();
            if (availableBackends.TryGetValue(hash, out RenderingBackend backend))
            {
                ReadOnlySpan<DestinationExtension> extensions = entity.GetArray<DestinationExtension>(destinationExtensionType);
                (MemoryAddress machine, MemoryAddress instance) = backend.create.Invoke(backend.allocation, entity, extensions);
                RenderingMachine newRenderingMachine = new(backend, machine, instance);
                renderingMachines.Add(entity, newRenderingMachine);
                knownDestinations.Add(entity);
                entity.AddComponent(rendererInstanceType, new RendererInstanceInUse(instance));
                Trace.WriteLine($"Created render system for destination `{destination}` with label `{label}`");
            }
            else
            {
                throw new InvalidOperationException($"Unknown renderer label `{label}`, no rendering backends available to handle it");
            }
        }

        private readonly void CollectComponents(World world, int rendererType, int materialType, int shaderType, int meshType)
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

        private readonly void CollectRenderers(World world, int viewportType)
        {
            //reset lists
            foreach (RenderingMachine renderingMachine in renderingMachines.Values)
            {
                foreach (ViewportGroup group in renderingMachine.viewportGroups.Values)
                {
                    foreach (List<RenderEntity> renderEntities in group.map.Values)
                    {
                        renderEntities.Clear();
                    }
                }
            }

            //collect renderers and store them with the viewports that they render to
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(viewportType) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsViewport> components = chunk.GetComponents<IsViewport>(viewportType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        uint viewportEntity = entities[i];
                        IsViewport viewport = components[i];
                        uint destinationEntity = world.GetReference(viewportEntity, viewport.destinationReference);
                        if (!world.ContainsEntity(destinationEntity))
                        {
                            continue;
                        }

                        Destination destination = Entity.Get<Destination>(world, destinationEntity);
                        ref RenderingMachine renderingMachine = ref renderingMachines.TryGetValue(destination, out bool containsRenderingMachine);
                        if (containsRenderingMachine)
                        {
                            ref ViewportGroup viewportGroup = ref renderingMachine.viewportGroups.TryGetValue(viewportEntity, out bool containsViewportGroup);
                            if (!containsViewportGroup)
                            {
                                viewportGroup = ref renderingMachine.viewportGroups.Add(viewportEntity);
                                viewportGroup = new();
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
                                                RenderEntity renderEntity = new(entity, meshEntity, vertexShaderEntity, fragmentShaderEntity, mesh.version, vertexShader.version, fragmentShader.version);
                                                MaterialData materialData = new(materialEntity, material);
                                                ref List<RenderEntity> renderEntities = ref viewportGroup.map.TryGetValue(materialData, out bool containsMaterialGroup);
                                                if (!containsMaterialGroup)
                                                {
                                                    renderEntities = ref viewportGroup.map.Add(materialData);
                                                    renderEntities = new();
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

        private readonly void CollectPlugins(World world, int pluginType)
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

        private readonly void CreateSurfaces(World world, int surfaceInUseType)
        {
            foreach (Destination destination in knownDestinations)
            {
                if (destination.world != world)
                {
                    continue;
                }

                //notify the entity that surface has been created
                ref RenderingMachine renderingMachine = ref renderingMachines[destination];
                if (!renderingMachine.hasSurface)
                {
                    renderingMachine.hasSurface = true;
                    if (destination.ContainsComponent(surfaceInUseType))
                    {
                        SurfaceInUse surfaceInUse = destination.GetComponent<SurfaceInUse>(surfaceInUseType);
                        renderingMachine.SurfaceCreated(surfaceInUse.value);
                    }
                    else
                    {
                        Trace.WriteLine($"Could not notify surface creation for destination `{destination}` because it does not have a surface in use component");
                    }
                }
            }
        }

        private readonly void Render(World world, int destinationType, int surfaceInUseType)
        {
            ReadOnlySpan<Destination> knownDestinations = this.knownDestinations.AsSpan();
            foreach (Destination destination in knownDestinations)
            {
                if (destination.world == world)
                {
                    if (!destination.ContainsComponent(surfaceInUseType))
                    {
                        continue; //no surface to render to yet
                    }

                    ref IsDestination component = ref destination.GetComponent<IsDestination>(destinationType);
                    if (component.Area == 0)
                    {
                        continue; //no area to render to
                    }

                    RenderingMachine renderingMachine = renderingMachines[destination];
                    StatusCode statusCode = renderingMachine.BeginRender(component.clearColor);
                    if (statusCode != StatusCode.Continue)
                    {
                        Trace.WriteLine($"Failed to begin rendering for destination `{destination}` because of status code `{statusCode}`");
                        return;
                    }

                    RenderDestination(world, renderingMachine);
                }
            }
        }

        private readonly void RenderDestination(World world, RenderingMachine renderingMachine)
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
                RenderViewport(world, renderingMachine, viewportEntity, viewportGroup);
            }

            renderingMachine.EndRender();
        }

        private readonly void RenderViewport(World world, RenderingMachine renderingMachine, uint viewportEntity, ViewportGroup viewportGroup)
        {
            //sort material groups by their declared order
            Span<(MaterialData materialData, List<RenderEntity> renderEntities)> materialGroups = stackalloc (MaterialData, List<RenderEntity>)[viewportGroup.map.Count];
            int materialCount = 0;
            foreach ((MaterialData materialData, List<RenderEntity> renderEntities) in viewportGroup.map)
            {
                materialGroups[materialCount++] = (materialData, renderEntities);
            }

            materialGroups.Sort(SortByMaterialOrder);

            //render all materials
            ReadOnlySpan<RenderEnginePluginFunction> pluginFunctions = this.pluginFunctions.AsSpan();
            for (int m = 0; m < materialCount; m++)
            {
                //preprocess
                (MaterialData materialData, List<RenderEntity> renderEntities) = materialGroups[m];
                uint materialEntity = materialData.entity;
                Span<RenderEntity> entities = renderEntities.AsSpan();
                for (int p = 0; p < pluginFunctions.Length; p++)
                {
                    pluginFunctions[p].Invoke(world, materialEntity, entities);
                }

                renderingMachine.Render(materialEntity, materialData.version, entities);
            }
        }

        private readonly void DestroyOldSystems(World world)
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
                    renderingMachines.Remove(destination, out RenderingMachine machine);
                    machine.Dispose();
                    knownDestinations.RemoveAt(i);
                    Trace.WriteLine($"Removed render system for destination `{destination}`");
                }
            }
        }

        private static int SortByViewportOrder((uint, ViewportGroup group) x, (uint, ViewportGroup group) y)
        {
            return x.group.order.CompareTo(y.group.order);
        }

        private static int SortByMaterialOrder((MaterialData material, List<RenderEntity>) x, (MaterialData material, List<RenderEntity>) y)
        {
            return x.material.order.CompareTo(y.material.order);
        }
    }
}