using Collections;
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
        private readonly Dictionary<FixedString, RenderingBackend> availableBackends;
        private readonly Dictionary<Destination, RenderingMachine> renderSystems;
        private readonly List<ViewportData> viewportEntities;
        private readonly List<Dictionary<RendererKey, List<uint>>> rendererGroups;
        private readonly Array<IsMaterial> materialComponents;
        private readonly Array<IsShader> shaderComponents;
        private readonly Array<IsMesh> meshComponents;

        private RenderEngineSystem(World world)
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

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Write(new RenderEngineSystem(world));
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            ComponentType destinationType = world.Schema.GetComponentType<IsDestination>();
            ComponentType rendererType = world.Schema.GetComponentType<IsRenderer>();
            ComponentType viewportType = world.Schema.GetComponentType<IsViewport>();
            ComponentType materialType = world.Schema.GetComponentType<IsMaterial>();
            ComponentType shaderType = world.Schema.GetComponentType<IsShader>();
            ComponentType meshType = world.Schema.GetComponentType<IsMesh>();
            TagType disabledTag = TagType.Disabled;
            DestroyOldSystems(world);
            CreateNewSystems(world, destinationType);
            FindViewports(world, viewportType, disabledTag);
            CollectComponents(world, materialType, shaderType, meshType);
            FindRenderers(world, rendererType, materialType, shaderType, meshType, disabledTag);
            Render(world, destinationType, shaderType, meshType);
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                meshComponents.Dispose();
                shaderComponents.Dispose();
                materialComponents.Dispose();

                foreach (Destination key in renderSystems.Keys)
                {
                    ref RenderingMachine renderSystem = ref renderSystems[key];
                    renderSystem.Dispose();
                }

                viewportEntities.Dispose();
                renderSystems.Dispose();
                knownDestinations.Dispose();

                foreach (FixedString label in availableBackends.Keys)
                {
                    ref RenderingBackend rendererBackend = ref availableBackends[label];
                    rendererBackend.Dispose();
                }

                availableBackends.Dispose();
                rendererGroups.Dispose();
            }
        }

        /// <summary>
        /// Makes the given render system type available for use at runtime,
        /// for destinations that reference its label.
        /// </summary>
        public readonly void RegisterRenderingBackend<T>() where T : unmanaged, IRenderingBackend
        {
            FixedString label = default(T).Label;
            if (availableBackends.ContainsKey(label))
            {
                throw new InvalidOperationException($"Label `{label}` already has a render system registered for");
            }

            RenderingBackend systemCreator = RenderingBackend.Create<T>();
            availableBackends.Add(label, systemCreator);
        }

        private readonly void CreateNewRenderers(World world, ComponentType destinationType)
        {
            USpan<FixedString> extensionNames = stackalloc FixedString[32];
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(destinationType))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsDestination> components = chunk.GetComponents<IsDestination>(destinationType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        ref IsDestination component = ref components[i];
                        Destination destination = new Entity(world, entities[i]).As<Destination>();
                        if (knownDestinations.Contains(destination))
                        {
                            return;
                        }

                        FixedString label = component.rendererLabel;
                        if (availableBackends.TryGetValue(label, out RenderingBackend renderingBackend))
                        {
                            uint extensionNamesLength = destination.CopyExtensionNamesTo(extensionNames);
                            (Allocation renderer, Allocation instance) = renderingBackend.create.Invoke(renderingBackend.allocation, destination, extensionNames.GetSpan(extensionNamesLength));
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

        private readonly void CollectComponents(World world, ComponentType materialType, ComponentType shaderType, ComponentType meshType)
        {
            uint capacity = Allocations.GetNextPowerOf2(world.MaxEntityValue + 1);
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
                USpan<uint> entities = chunk.Entities;
                if (definition.ContainsComponent(materialType))
                {
                    USpan<IsMaterial> components = chunk.GetComponents<IsMaterial>(materialType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        materialComponents[entities[i]] = components[i];
                    }
                }

                if (definition.ContainsComponent(shaderType))
                {
                    USpan<IsShader> components = chunk.GetComponents<IsShader>(shaderType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        shaderComponents[entities[i]] = components[i];
                    }
                }

                if (definition.ContainsComponent(meshType))
                {
                    USpan<IsMesh> components = chunk.GetComponents<IsMesh>(meshType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        meshComponents[entities[i]] = components[i];
                    }
                }
            }
        }

        private readonly void FindRenderers(World world, ComponentType rendererType, ComponentType materialType, ComponentType shaderType, ComponentType meshType, TagType disabledTag)
        {
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(rendererType) && !chunk.Definition.ContainsTag(disabledTag))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsRenderer> components = chunk.GetComponents<IsRenderer>(rendererType);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        ref IsRenderer component = ref components[i];
                        uint entity = entities[i];
                        rint materialReference = component.materialReference;
                        uint materialEntity = world.GetReference(entity, materialReference);
                        IsMaterial materialComponent = materialComponents[materialEntity];
                        if (materialComponent == default)
                        {
                            continue; //material not yet loaded
                        }

                        uint vertexShaderEntity = world.GetReference(materialEntity, materialComponent.vertexShaderReference);
                        IsShader vertexShaderComponent = shaderComponents[vertexShaderEntity];
                        if (vertexShaderComponent == default)
                        {
                            continue; //vertex shader not yet loaded
                        }

                        uint fragmentShaderEntity = world.GetReference(materialEntity, materialComponent.fragmentShaderReference);
                        IsShader fragmentShaderComponent = shaderComponents[fragmentShaderEntity];
                        if (fragmentShaderComponent == default)
                        {
                            continue; //fragment shader not yet loaded
                        }

                        rint meshReference = component.meshReference;
                        uint meshEntity = world.GetReference(entity, meshReference);
                        IsMesh meshComponent = meshComponents[meshEntity];
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

        private readonly void FindViewports(World world, ComponentType viewportType, TagType disabledTag)
        {
            //reset viewport lists
            viewportEntities.Clear();
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Definition.ContainsComponent(viewportType) && !chunk.Definition.ContainsTag(disabledTag))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsViewport> components = chunk.GetComponents<IsViewport>(viewportType);
                    for (uint i = 0; i < entities.Length; i++)
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

        private readonly void CreateNewSystems(World world, ComponentType destinationType)
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
                if (!renderSystem.IsSurfaceAvailable && destination.TryGetSurfaceInUse(out Allocation surface))
                {
                    renderSystem.SurfaceCreated(surface);
                }
            }
        }

        private readonly void Render(World world, ComponentType destinationType, ComponentType shaderType, ComponentType meshType)
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
                        MeshData mesh = new(info.mesh, meshComponents[info.mesh].version);
                        VertexShaderData vertexShader = new(info.vertexShader, shaderComponents[info.vertexShader].version);
                        FragmentShaderData fragmentShader = new(info.fragmentShader, shaderComponents[info.fragmentShader].version);
                        renderSystem.Render(renderers.AsSpan(), material, mesh, vertexShader, fragmentShader);
                    }
                }

                rendererGroups.Clear();
                renderSystem.EndRender();
            }
        }

        private readonly void DestroyOldSystems(World world)
        {
            for (uint i = knownDestinations.Count - 1; i != uint.MaxValue; i--)
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