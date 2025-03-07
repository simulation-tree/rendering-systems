using Collections.Generic;
using Simulation;
using System;
using System.Numerics;
using Unmanaged;

namespace Rendering.Systems
{
    /// <summary>
    /// Represents a handler of <see cref="IRenderingBackend"/> functions for a specific <see cref="Destination"/>.
    /// </summary>
    public struct RenderingMachine : IDisposable
    {
        private bool hasSurface;
        public readonly List<Viewport> viewports;
        public readonly Dictionary<Viewport, Dictionary<RendererKey, List<uint>>> renderers;
        public readonly Dictionary<RendererKey, RendererCombination> infos;

        private readonly MemoryAddress allocation;
        private readonly RenderingBackend backend;

        public readonly bool IsSurfaceAvailable => hasSurface;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public RenderingMachine()
        {
            throw new NotImplementedException();
        }
#endif

        internal RenderingMachine(MemoryAddress allocation, RenderingBackend backend)
        {
            this.allocation = allocation;
            this.backend = backend;
            hasSurface = false;

            viewports = new();
            renderers = new();
            infos = new();
        }

        public readonly void Dispose()
        {
            backend.dispose.Invoke(backend.allocation, allocation);
            infos.Dispose();
            viewports.Dispose();

            foreach (Viewport viewport in renderers.Keys)
            {
                Dictionary<RendererKey, List<uint>> groups = renderers[viewport];
                foreach (RendererKey hash in groups.Keys)
                {
                    List<uint> renderers = groups[hash];
                    renderers.Dispose();
                }

                groups.Dispose();
            }

            renderers.Dispose();
        }

        public void SurfaceCreated(MemoryAddress surface)
        {
            backend.surfaceCreated.Invoke(backend.allocation, allocation, surface);
            hasSurface = true;
        }

        public readonly StatusCode BeginRender(Vector4 clearColor)
        {
            return backend.beginRender.Invoke(backend.allocation, allocation, clearColor);
        }

        public readonly void Render(USpan<uint> renderers, MaterialData material, MeshData mesh, VertexShaderData vertexShader, FragmentShaderData fragmentShader)
        {
            backend.render.Invoke(backend.allocation, allocation, renderers, material, mesh, vertexShader, fragmentShader);
        }

        public readonly void EndRender()
        {
            backend.endRender.Invoke(backend.allocation, allocation);
        }
    }
}