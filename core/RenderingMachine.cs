using Simulation;
using System;
using System.Numerics;
using Unmanaged;

namespace Rendering.Systems
{
    /// <summary>
    /// Represents a handler of <see cref="IRenderingBackend"/> functions for a specific <see cref="Destination"/>.
    /// </summary>
    internal struct RenderingMachine : IDisposable
    {
        public readonly RendererGroups rendererGroups;
        private bool hasSurface;

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
            rendererGroups = new();
            this.allocation = allocation;
            this.backend = backend;
            hasSurface = false;
        }

        public readonly void Dispose()
        {
            backend.dispose.Invoke(backend.allocation, allocation);
            rendererGroups.Dispose();
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

        public readonly void Render(ReadOnlySpan<uint> renderers, MaterialData material, MeshData mesh, VertexShaderData vertexShader, FragmentShaderData fragmentShader)
        {
            backend.render.Invoke(backend.allocation, allocation, renderers, material, mesh, vertexShader, fragmentShader);
        }

        public readonly void EndRender()
        {
            backend.endRender.Invoke(backend.allocation, allocation);
        }
    }
}