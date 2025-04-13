using Collections.Generic;
using Materials.Components;
using Meshes.Components;
using Rendering.Components;
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
        private readonly ViewportGroups viewportGroups;

        public RenderEngineSystem()
        {
            knownDestinations = new();
            availableBackends = new();
            renderingMachines = new();
            entityComponents = new();
            viewportGroups = new();
        }

        public readonly void Dispose()
        {
            viewportGroups.Dispose();
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
            int rendererInstanceType = schema.GetComponentType<RendererInstanceInUse>();
            int destinationExtensionType = schema.GetArrayType<DestinationExtension>();
            DestroyOldSystems(world);
            CreateNewSystems(world, destinationType, rendererInstanceType, destinationExtensionType);
            CollectComponents(world, rendererType, materialType, shaderType, meshType);
            CollectRenderers(world, viewportType);
            Render(world, destinationType);
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

        private readonly void CreateNewRenderers(World world, int destinationType, int rendererInstanceType, int destinationExtensionType)
        {
            Span<ASCIIText256> extensionNames = stackalloc ASCIIText256[32];
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
                        Destination entity = new Entity(world, entities[i]).As<Destination>();
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
            if (availableBackends.TryGetValue(hash, out RenderingBackend renderingBackend))
            {
                ReadOnlySpan<DestinationExtension> extensions = entity.GetArray<DestinationExtension>(destinationExtensionType);
                (MemoryAddress renderer, MemoryAddress instance) = renderingBackend.create.Invoke(renderingBackend.allocation, entity, extensions);
                RenderingMachine newRenderingMachine = new(renderer, renderingBackend);
                renderingMachines.Add(entity, newRenderingMachine);
                knownDestinations.Add(entity);
                entity.AddComponent(rendererInstanceType, new RendererInstanceInUse(instance));
                Trace.WriteLine($"Created render system for destination `{destination}` with label `{label}`");
            }
            else
            {
                throw new InvalidOperationException($"Unknown renderer label `{label}`");
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
            int viewportCount = 0;
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(viewportType) && !definition.ContainsTag(Schema.DisabledTagType))
                {
                    viewportCount += chunk.Count;
                }
            }

            //find all viewports
            Span<(uint viewportEntity, IsViewport component, RenderingMachine renderingMachine)> viewports = stackalloc (uint, IsViewport, RenderingMachine)[viewportCount];
            viewportCount = 0;
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
                        IsViewport component = components[i];
                        Viewport entity = new Entity(world, viewportEntity).As<Viewport>();
                        uint destinationEntity = entity.GetReference(component.destinationReference);
                        if (!world.ContainsEntity(destinationEntity))
                        {
                            continue;
                        }

                        Destination destination = new Entity(world, destinationEntity).As<Destination>();
                        ref RenderingMachine renderingMachine = ref renderingMachines.TryGetValue(destination, out bool contains);
                        if (contains)
                        {
                            viewports[viewportCount++] = (viewportEntity, component, renderingMachine);
                            renderingMachine.rendererGroups.Clear();
                        }
                        else
                        {
                            //system with label not found
                        }
                    }
                }
            }

            viewports = viewports.Slice(0, viewportCount);
            viewports.Sort(SortViewportsByViewportOrder);

            this.viewportGroups.EnsureCapacity(viewportCount);

            //collect renderers for each viewport
            Span<EntityComponents> entityComponents = this.entityComponents.AsSpan();
            Span<ViewportGroup> viewportGroups = this.viewportGroups.AsSpan();
            for (int v = 0; v < viewportCount; v++)
            {
                ref ViewportGroup viewportGroup = ref viewportGroups[v];
                (uint viewportEntity, IsViewport component, RenderingMachine renderingMachine) = viewports[v];
                viewportGroup.Initialize(viewportEntity);
                LayerMask viewportMask = component.renderMask;
                for (uint e = 0; e < entityComponents.Length; e++)
                {
                    EntityComponents components = entityComponents[(int)e];
                    if (components.ContainsRenderer)
                    {
                        IsRenderer renderer = components.renderer;
                        if (renderer.renderMask.ContainsAny(viewportMask))
                        {
                            if (renderer.materialReference != default && renderer.meshReference != default)
                            {
                                uint materialEntity = world.GetReference(e, renderer.materialReference);
                                IsMaterial material = entityComponents[(int)materialEntity].material;
                                viewportGroup.Add(e, materialEntity, material);
                            }
                        }
                    }
                }

                Span<(uint rendererEntity, uint materialEntity, IsMaterial material)> rendererEntities = viewportGroup.Renderers;
                rendererEntities.Sort(SortRenderersByMaterialOrder);

                for (int i = 0; i < rendererEntities.Length; i++)
                {
                    (uint rendererEntity, uint materialEntity, IsMaterial material) = rendererEntities[i];
                    IsRenderer renderer = entityComponents[(int)rendererEntity].renderer;
                    uint meshEntity = world.GetReference(rendererEntity, renderer.meshReference);
                    IsMesh mesh = entityComponents[(int)meshEntity].mesh;
                    if (material.vertexShaderReference != default && material.fragmentShaderReference != default)
                    {
                        uint vertexShaderEntity = world.GetReference(materialEntity, material.vertexShaderReference);
                        IsShader vertexShader = entityComponents[(int)vertexShaderEntity].shader;
                        uint fragmentShaderEntity = world.GetReference(materialEntity, material.fragmentShaderReference);
                        IsShader fragmentShader = entityComponents[(int)fragmentShaderEntity].shader;
                        RendererCombination combination = new(materialEntity, meshEntity, vertexShaderEntity, fragmentShaderEntity);
                        renderingMachine.rendererGroups.Add(combination, rendererEntity);
                    }
                }
            }
        }

        private readonly void CreateNewSystems(World world, int destinationType, int rendererInstanceType, int destinationExtensionType)
        {
            CreateNewRenderers(world, destinationType, rendererInstanceType, destinationExtensionType);

            foreach (Destination destination in knownDestinations)
            {
                if (destination.world != world)
                {
                    continue;
                }

                //notify that surface has been created
                ref RenderingMachine renderingMachine = ref renderingMachines[destination];
                if (!renderingMachine.IsSurfaceAvailable && destination.TryGetSurfaceInUse(out MemoryAddress surface))
                {
                    renderingMachine.SurfaceCreated(surface);
                }
            }
        }

        private readonly void Render(World world, int destinationType)
        {
            foreach (Destination destination in knownDestinations)
            {
                if (destination.world == world)
                {
                    if (!destination.ContainsComponent<SurfaceInUse>())
                    {
                        continue; //no surface to render to yet
                    }

                    ref IsDestination component = ref destination.GetComponent<IsDestination>(destinationType);
                    if (component.Area == 0)
                    {
                        continue; //no area to render to
                    }

                    ref RenderingMachine renderingMachine = ref renderingMachines[destination];
                    StatusCode statusCode = renderingMachine.BeginRender(component.clearColor);
                    if (statusCode != StatusCode.Continue)
                    {
                        Trace.WriteLine($"Failed to begin rendering for destination `{destination}` because of status code `{statusCode}`");
                        continue;
                    }

                    ReadOnlySpan<RendererCombination> combinations = renderingMachine.rendererGroups.Combinations;
                    for (int c = 0; c < combinations.Length; c++)
                    {
                        RendererCombination combination = combinations[c];
                        ReadOnlySpan<uint> entities = renderingMachine.rendererGroups.GetEntities(combination);
                        MaterialData material = new(combination.materialEntity, GetMaterialVersion(combination.materialEntity));
                        MeshData mesh = new(combination.meshEntity, GetMeshVersion(combination.meshEntity));
                        VertexShaderData vertexShader = new(combination.vertexShaderEntity, GetShaderVersion(combination.vertexShaderEntity));
                        FragmentShaderData fragmentShader = new(combination.fragmentShaderEntity, GetShaderVersion(combination.fragmentShaderEntity));
                        renderingMachine.Render(entities, material, mesh, vertexShader, fragmentShader);
                    }

                    renderingMachine.EndRender();
                }
            }
        }

        private readonly uint GetMaterialVersion(uint entity)
        {
            return entityComponents[(int)entity].material.version;
        }

        private readonly uint GetMeshVersion(uint entity)
        {
            return entityComponents[(int)entity].mesh.version;
        }

        private readonly uint GetShaderVersion(uint entity)
        {
            return entityComponents[(int)entity].shader.version;
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
                    renderingMachines.Remove(destination, out RenderingMachine destinationRenderer);
                    destinationRenderer.Dispose();
                    knownDestinations.RemoveAt(i);
                    Trace.WriteLine($"Removed render system for destination `{destination}`");
                }
            }
        }

        private static int SortViewportsByViewportOrder((uint, IsViewport viewport, RenderingMachine) x, (uint, IsViewport viewport, RenderingMachine) y)
        {
            return x.viewport.order.CompareTo(y.viewport.order);
        }

        private static int SortRenderersByMaterialOrder((uint, uint, IsMaterial material) x, (uint, uint, IsMaterial material) y)
        {
            return x.material.order.CompareTo(y.material.order);
        }
    }
}