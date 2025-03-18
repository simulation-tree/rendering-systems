using Rendering.Functions;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Rendering
{
    /// <summary>
    /// Defines a <see cref="IRenderingBackend"/> type using functions.
    /// </summary>
    public readonly struct RenderingBackend : IDisposable
    {
        public readonly MemoryAddress allocation;
        public readonly ASCIIText256 label;
        public readonly Create create;
        public readonly Dispose dispose;
        public readonly SurfaceCreated surfaceCreated;
        public readonly BeginRender beginRender;
        public readonly Render render;
        public readonly EndRender endRender;

        private readonly FinishFunction finalize;

        private RenderingBackend(MemoryAddress allocation, ASCIIText256 label, StartFunction startFunction, FinishFunction finalize, Create create, Dispose dispose, SurfaceCreated surfaceCreated, BeginRender beginRender, Render render, EndRender endRender)
        {
            this.allocation = allocation;
            this.label = label;
            this.finalize = finalize;
            this.create = create;
            this.dispose = dispose;
            this.render = render;
            this.surfaceCreated = surfaceCreated;
            this.beginRender = beginRender;
            this.endRender = endRender;

            startFunction.Invoke(allocation);
        }

        /// <summary>
        /// Disposes of the rendering backend by calling the <see cref="FinishFunction"/> function.
        /// </summary>
        public void Dispose()
        {
            finalize.Invoke(allocation);
            allocation.Dispose();
        }

        public static RenderingBackend Create<T>() where T : unmanaged, IRenderingBackend
        {
            T v = default;
            ThrowIfDefault(v);

            StartFunction startFunction = v.StartFunction;
            FinishFunction finalize = v.FinishFunction;
            Create create = v.CreateFunction;
            Dispose dispose = v.DisposeFunction;
            Render render = v.RenderFunction;
            SurfaceCreated surfaceCreated = v.SurfaceCreatedFunction;
            BeginRender beginRender = v.BeginRenderFunction;
            EndRender endRender = v.EndRenderFunction;
            MemoryAddress rendererBackend = MemoryAddress.AllocateValue(v);
            return new(rendererBackend, v.Label, startFunction, finalize, create, dispose, surfaceCreated, beginRender, render, endRender);
        }

        public static ASCIIText256 GetLabel<T>() where T : unmanaged, IRenderingBackend
        {
            return default(T).Label;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfDefault<T>(T backend) where T : unmanaged, IRenderingBackend
        {
            if (backend.StartFunction == default)
            {
                throw new InvalidOperationException($"Rendering backend {typeof(T)} doesn't have functions implemented");
            }
        }
    }
}