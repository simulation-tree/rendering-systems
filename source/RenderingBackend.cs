using System;

namespace Rendering.Systems
{
    public abstract class RenderingBackend : IDisposable
    {
        public abstract ReadOnlySpan<char> Label { get; }

        public RenderingBackend()
        {
        }

        public abstract void Dispose();
        public abstract RenderingMachine CreateRenderingMachine(Destination destination);
    }
}