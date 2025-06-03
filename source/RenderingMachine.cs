using Collections.Generic;
using System;
using System.Numerics;
using Unmanaged;
using Worlds;

namespace Rendering.Systems
{
    /// <summary>
    /// A system that renders entities to the <see cref="Destination"/>.
    /// </summary>
    public abstract class RenderingMachine
    {
        internal RenderingBackend? renderingBackend;
        internal readonly Dictionary<uint, ViewportGroup> viewportGroups;

        /// <summary>
        /// The world this rendering machine is associated with.
        /// </summary>
        public readonly World world;

        /// <summary>
        /// The destination this rendering machine is responsible for.
        /// </summary>
        public readonly Destination destination;

        /// <summary>
        /// The instance of the rendering backend.
        /// </summary>
        public readonly MemoryAddress instance;

        public RenderingMachine(Destination destination, MemoryAddress instance)
        {
            this.world = destination.world;
            this.destination = destination;
            this.instance = instance;
            viewportGroups = new(4);
        }

        /// <summary>
        /// Callback when the destination has had a surface created for it.
        /// </summary>
        public abstract void SurfaceCreated(MemoryAddress surface);

        public abstract bool BeginRender(Vector4 clearColor);
        public abstract void Render(sbyte renderGroup, ReadOnlySpan<RenderEntity> renderers);
        public abstract void EndRender();
    }
}