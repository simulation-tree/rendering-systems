using Collections.Generic;
using System;
using System.Numerics;
using Unmanaged;

namespace Rendering.Systems
{
    /// <summary>
    /// A system that renders entities to the <see cref="Destination"/>.
    /// </summary>
    public abstract class RenderingMachine : IDisposable
    {
        public readonly Destination destination;

        internal readonly Dictionary<uint, ViewportGroup> viewportGroups;

        public abstract MemoryAddress Instance { get; }

        public RenderingMachine(Destination destination)
        {
            viewportGroups = new();
            this.destination = destination;
        }

        public abstract void Dispose();
        public abstract void SurfaceCreated(MemoryAddress surface);
        public abstract bool BeginRender(Vector4 clearColor);
        public abstract void Render(sbyte renderGroup, ReadOnlySpan<RenderEntity> renderers);
        public abstract void EndRender();
    }
}