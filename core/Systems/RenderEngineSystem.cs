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
        private readonly Dictionary<ASCIIText256, RenderingBackend> availableBackends;
        private readonly Dictionary<Destination, RenderingMachine> renderSystems;
        private readonly List<ViewportData> viewportEntities;
        private readonly List<Dictionary<RendererKey, List<uint>>> rendererGroups;
        private readonly Array<IsMaterial> materialComponents;
        private readonly Array<IsShader> shaderComponents;
        private readonly Array<IsMesh> meshComponents;

        public RenderEngineSystem()
        {
            knownDestinations = new();
            availableBackends = new();
            renderSystems = new();
            viewportEntities = new();
            rendererGroups = new();
            materialComponents = new();
            shaderComponents = new();
            meshComponents = new();
        }

        public readonly void Dispose()
        {
            meshComponents.Dispose();
            shaderComponents.Dispose();
            materialComponents.Dispose();

            foreach (RenderingMachine renderSystem in renderSystems.Values)
            {
                renderSystem.Dispose();
            }

            viewportEntities.Dispose();
            renderSystems.Dispose();
            knownDestinations.Dispose();

            foreach (RenderingBackend rendererBackend in availableBackends.Values)
            {
                rendererBackend.Dispose();
            }

            availableBackends.Dispose();
            rendererGroups.Dispose();
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            int destinationType = world.Schema.GetComponentType<IsDestination>();
            int rendererType = world.Schema.GetComponentType<IsRenderer>();
            int viewportType = world.Schema.GetComponentType<IsViewport>();
            int materialType = world.Schema.GetComponentType<IsMaterial>();
            int shaderType = world.Schema.GetComponentType<IsShader>();
            int meshType = world.Schema.GetComponentType<IsMesh>();
            DestroyOldSystems(world);
            CreateNewSystems(world, destinationType);
            FindViewports(world, viewportType);
            CollectComponents(world, materialType, shaderType, meshType);
            FindRenderers(world, rendererType);
            Render(world, destinationType, shaderType, meshType);
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }

        /// <summary>
        /// Makes the given render system type available for use at runtime,
        /// for destinations that reference its label.
        /// </summary>
        public readonly void RegisterRenderingBackend<T>() where T : unmanaged, IRenderingBackend
        {
            ASCIIText256 label = default(T).Label;
            if (availableBackends.ContainsKey(label))
            {
                throw new InvalidOperationException($"Label `{label}` already has a render system registered for");
            }

            RenderingBackend systemCreator = RenderingBackend.Create<T>();
            availableBackends.Add(label, systemCreator);
        }

        private readonly void CreateNewRenderers(World world, int destinationType)
        {
            Span<ASCIIText256> extensionNames = stackalloc ASCIIText256[32];
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(destinationType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsDestination> components = chunk.GetComponents<IsDestination>(destinationType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsDestination component = ref components[i];
                        Destination destination = new Entity(world, entities[i]).As<Destination>();
                        if (knownDestinations.Contains(destination))
                        {
                            return;
                        }

                        ASCIIText256 label = component.rendererLabel;
                        if (availableBackends.TryGetValue(label, out RenderingBackend renderingBackend))
                        {
                            int extensionNamesLength = destination.CopyExtensionNamesTo(extensionNames);
                            (MemoryAddress renderer, MemoryAddress instance) = renderingBackend.create.Invoke(renderingBackend.allocation, destination, extensionNames.Slice(0, extensionNamesLength));
                            RenderingMachine newRenderSystem = new(renderer, renderingBackend);
                            renderSystems.Add(destination, newRenderSystem);
                            knownDestinations.Add(destination);
                            destination.AddComponent(new RendererInstanceInUse(instance));
                            Trace.WriteLine($"Created render system for destination `{destination}` with label `{label}`");
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unknown renderer label `{label}`");
                        }
                    }
                }
            }
        }

        private readonly void CollectComponents(World world, int materialType, int shaderType, int meshType)
        {
            int capacity = (world.MaxEntityValue + 1).GetNextPowerOf2();
            if (materialComponents.Length < capacity)
            {
                materialComponents.Length = capacity;
            }

            if (shaderComponents.Length < capacity)
            {
                shaderComponents.Length = capacity;
            }

            if (meshComponents.Length < capacity)
            {
                meshComponents.Length = capacity;
            }

            materialComponents.Clear();
            shaderComponents.Clear();
            meshComponents.Clear();

            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                ReadOnlySpan<uint> entities = chunk.Entities;
                if (definition.ContainsComponent(materialType))
                {
                    ComponentEnumerator<IsMaterial> components = chunk.GetComponents<IsMaterial>(materialType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        materialComponents[(int)entities[i]] = components[i];
                    }
                }

                if (definition.ContainsComponent(shaderType))
                {
                    ComponentEnumerator<IsShader> components = chunk.GetComponents<IsShader>(shaderType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        shaderComponents[(int)entities[i]] = components[i];
                    }
                }

                if (definition.ContainsComponent(meshType))
                {
                    ComponentEnumerator<IsMesh> components = chunk.GetComponents<IsMesh>(meshType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        meshComponents[(int)entities[i]] = components[i];
                    }
                }
            }
        }

        private readonly void FindRenderers(World world, int rendererType)
        {
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(rendererType) && !chunk.Definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsRenderer> components = chunk.GetComponents<IsRenderer>(rendererType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsRenderer component = ref components[i];
                        uint entity = entities[i];
                        rint materialReference = component.materialReference;
                        uint materialEntity = world.GetReference(entity, materialReference);
                        IsMaterial materialComponent = materialComponents[(int)materialEntity];
                        if (materialComponent == default)
                        {
                            continue; //material not yet loaded
                        }

                        uint vertexShaderEntity = world.GetReference(materialEntity, materialComponent.vertexShaderReference);
                        IsShader vertexShaderComponent = shaderComponents[(int)vertexShaderEntity];
                        if (vertexShaderComponent == default)
                        {
                            continue; //vertex shader not yet loaded
                        }

                        uint fragmentShaderEntity = world.GetReference(materialEntity, materialComponent.fragmentShaderReference);
                        IsShader fragmentShaderComponent = shaderComponents[(int)fragmentShaderEntity];
                        if (fragmentShaderComponent == default)
                        {
                            continue; //fragment shader not yet loaded
                        }

                        rint meshReference = component.meshReference;
                        uint meshEntity = world.GetReference(entity, meshReference);
                        IsMesh meshComponent = meshComponents[(int)meshEntity];
                        if (meshComponent == default)
                        {
                            continue; //mesh not yet loaded
                        }

                        LayerMask renderMask = component.renderMask;

                        //for each viewport, add this renderer if it intersects with their render mak
                        foreach (ViewportData viewport in viewportEntities)
                        {
                            if (viewport.renderMask.ContainsAny(renderMask))
                            {
                                if (renderSystems.TryGetValue(viewport.destination, out RenderingMachine renderSystem))
                                {
                                    if (!renderSystem.renderers.TryGetValue(viewport.entity, out Dictionary<RendererKey, List<uint>> groups))
                                    {
                                        groups = new();
                                        renderSystem.renderers.Add(viewport.entity, groups);
                                    }

                                    RendererKey key = new(materialEntity, meshEntity);
                                    if (!groups.TryGetValue(key, out List<uint> renderers))
                                    {
                                        renderers = new();
                                        groups.Add(key, renderers);
                                        renderSystem.infos.AddOrSet(key, new(materialEntity, meshEntity, vertexShaderEntity, fragmentShaderEntity));
                                    }

                                    renderers.Add(entity);
                                }
                            }
                        }
                    }
                }
            }
        }

        private readonly void FindViewports(World world, int viewportType)
        {
            //reset viewport lists
            viewportEntities.Clear();
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(viewportType) && !chunk.Definition.ContainsTag(Schema.DisabledTagType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsViewport> components = chunk.GetComponents<IsViewport>(viewportType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        ref IsViewport component = ref components[i];
                        Viewport viewport = new Entity(world, entities[i]).As<Viewport>();
                        Destination destination = viewport.Destination;
                        if (renderSystems.TryGetValue(destination, out RenderingMachine destinationRenderer))
                        {
                            destinationRenderer.viewports.Add(viewport);
                            renderSystems[destination] = destinationRenderer;
                        }
                        else
                        {
                            //system with label not found
                        }

                        viewportEntities.Add(new(viewport, component.renderMask, destination));
                    }
                }
            }
        }

        private readonly void CreateNewSystems(World world, int destinationType)
        {
            CreateNewRenderers(world, destinationType);

            //reset lists
            foreach (Destination destination in knownDestinations)
            {
                if (destination.world != world)
                {
                    continue;
                }

                ref RenderingMachine renderSystem = ref renderSystems[destination];
                renderSystem.viewports.Clear();

                foreach (Viewport viewport in renderSystem.renderers.Keys)
                {
                    Dictionary<RendererKey, List<uint>> renderersPerCamera = renderSystem.renderers[viewport];
                    foreach (RendererKey key in renderersPerCamera.Keys)
                    {
                        List<uint> renderers = renderersPerCamera[key];
                        renderers.Clear();
                    }
                }

                //notify that surface has been created
                if (!renderSystem.IsSurfaceAvailable && destination.TryGetSurfaceInUse(out MemoryAddress surface))
                {
                    renderSystem.SurfaceCreated(surface);
                }
            }
        }

        private readonly void Render(World world, int destinationType, int shaderType, int meshType)
        {
            foreach (Destination destination in knownDestinations)
            {
                if (destination.world != world)
                {
                    continue;
                }

                if (!destination.ContainsComponent<SurfaceInUse>())
                {
                    continue; //no surface to render to yet
                }

                ref IsDestination component = ref destination.GetComponent<IsDestination>(destinationType);
                if (component.Area == 0)
                {
                    continue; //no area to render to
                }

                ref RenderingMachine renderSystem = ref renderSystems[destination];
                StatusCode statusCode = renderSystem.BeginRender(component.clearColor);
                if (statusCode != StatusCode.Continue)
                {
                    Trace.WriteLine($"Failed to begin rendering for destination `{destination}` because of status code `{statusCode}`");
                    continue;
                }

                foreach (Viewport viewport in renderSystem.viewports)
                {
                    ref Dictionary<RendererKey, List<uint>> groups = ref renderSystem.renderers.TryGetValue(viewport, out bool containsGroups);
                    if (containsGroups)
                    {
                        rendererGroups.Add(groups);
                        //make sure renderer entries that no longer exist are not in this list
                        //todo: is this needed?
                        //foreach (RendererKey key in groups.Keys)
                        //{
                        //    ref List<uint> renderers = ref groups[key];
                        //    uint rendererCount = renderers.Count;
                        //    for (uint r = rendererCount - 1; r != uint.MaxValue; r--)
                        //    {
                        //        uint rendererEntity = renderers[r];
                        //        if (!world.ContainsEntity(rendererEntity))
                        //        {
                        //            renderers.RemoveAt(r);
                        //        }
                        //    }
                        //}
                    }
                }

                //todo: iterate with respect to each camera's sorting order
                foreach (Dictionary<RendererKey, List<uint>> groups in rendererGroups)
                {
                    foreach (RendererKey key in groups.Keys)
                    {
                        ref List<uint> renderers = ref groups[key];
                        ref RendererCombination info = ref renderSystem.infos[key];
                        MaterialData material = new(info.material, 0);
                        MeshData mesh = new(info.mesh, meshComponents[(int)info.mesh].version);
                        VertexShaderData vertexShader = new(info.vertexShader, shaderComponents[(int)info.vertexShader].version);
                        FragmentShaderData fragmentShader = new(info.fragmentShader, shaderComponents[(int)info.fragmentShader].version);
                        renderSystem.Render(renderers.AsSpan(), material, mesh, vertexShader, fragmentShader);
                    }
                }

                rendererGroups.Clear();
                renderSystem.EndRender();
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
                    renderSystems.Remove(destination, out RenderingMachine destinationRenderer);
                    destinationRenderer.Dispose();
                    knownDestinations.RemoveAt(i);
                    Trace.WriteLine($"Removed render system for destination `{destination}`");
                }
            }
        }
    }
}