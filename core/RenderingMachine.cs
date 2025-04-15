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
    internal struct RenderingMachine : IDisposable
    {
        public readonly Dictionary<uint, ViewportGroup> viewportGroups;
        public bool hasSurface;

        private readonly RenderingBackend backend;
        private readonly MemoryAddress machine;
        private readonly MemoryAddress instance;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public RenderingMachine()
        {
            throw new NotImplementedException();
        }
#endif

        internal RenderingMachine(RenderingBackend backend, MemoryAddress machine, MemoryAddress instance)
        {
            viewportGroups = new();
            this.backend = backend;
            this.machine = machine;
            this.instance = instance;
            hasSurface = false;
        }

        public readonly void Dispose()
        {
            backend.dispose.Invoke(backend.allocation, machine, instance);

            foreach (ViewportGroup group in viewportGroups.Values)
            {
                group.Dispose();
            }

            viewportGroups.Dispose();
        }

        public readonly void SurfaceCreated(MemoryAddress surface)
        {
            backend.surfaceCreated.Invoke(backend.allocation, machine, surface);
        }

        public readonly StatusCode BeginRender(Vector4 clearColor)
        {
            return backend.beginRender.Invoke(backend.allocation, machine, clearColor);
        }

        public readonly void Render(uint materialEntity, ushort materialVersion, ReadOnlySpan<RenderEntity> renderers)
        {
            backend.render.Invoke(backend.allocation, machine, materialEntity, materialVersion, renderers);
        }

        public readonly void EndRender()
        {
            backend.endRender.Invoke(backend.allocation, machine);
        }
    }
}