using System;
using System.Collections.Generic;

namespace Rendering.Systems
{
    public abstract class RenderingBackend : IDisposable
    {
        internal readonly List<RenderingMachine> renderingMachines = new();

        public abstract ReadOnlySpan<char> Label { get; }

        public abstract void Dispose();
        public abstract RenderingMachine CreateRenderingMachine(Destination destination);
        public abstract void DisposeRenderingMachine(RenderingMachine renderingMachine);
    }

    public abstract class RenderingBackend<T> : RenderingBackend where T : RenderingMachine
    {
        public sealed override RenderingMachine CreateRenderingMachine(Destination destination)
        {
            return Create(destination);
        }

        public sealed override void DisposeRenderingMachine(RenderingMachine renderingMachine)
        {
            Dispose((T)renderingMachine);
        }

        public abstract T Create(Destination destination);
        public abstract void Dispose(T renderingMachine);
    }
}