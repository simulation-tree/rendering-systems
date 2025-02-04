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
        public readonly Allocation allocation;
        public readonly FixedString label;
        public readonly Create create;
        public readonly Dispose dispose;
        public readonly SurfaceCreated surfaceCreated;
        public readonly BeginRender beginRender;
        public readonly Render render;
        public readonly EndRender endRender;

        private readonly Finalize finalize;

        private RenderingBackend(Allocation allocation, FixedString label, Initialize initialize, Finalize finalize, Create create, Dispose dispose, SurfaceCreated surfaceCreated, BeginRender beginRender, Render render, EndRender endRender)
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

            initialize.Invoke(allocation);
        }

        /// <summary>
        /// Disposes of the rendering backend by calling the <see cref="Finalize"/> function.
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

            Initialize initialize = v.InitializeFunction;
            Finalize finalize = v.FinalizeFunction;
            Create create = v.CreateFunction;
            Dispose dispose = v.DisposeFunction;
            Render render = v.RenderFunction;
            SurfaceCreated surfaceCreated = v.SurfaceCreatedFunction;
            BeginRender beginRender = v.BeginRenderFunction;
            EndRender endRender = v.EndRenderFunction;
            Allocation rendererBackend = Allocation.Create(v);
            return new(rendererBackend, v.Label, initialize, finalize, create, dispose, surfaceCreated, beginRender, render, endRender);
        }

        public static FixedString GetLabel<T>() where T : unmanaged, IRenderingBackend
        {
            return default(T).Label;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfDefault<T>(T backend) where T : unmanaged, IRenderingBackend
        {
            if (backend.InitializeFunction == default)
            {
                throw new InvalidOperationException($"Rendering backend {typeof(T)} doesn't have functions implemented");
            }
        }
    }
}