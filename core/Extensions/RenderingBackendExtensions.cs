using Rendering.Functions;
using Simulation;
using System.Numerics;
using Unmanaged;

namespace Rendering
{
    public static class RenderingBackendExtensions
    {
        public static void PerformStart<T>(ref T backend) where T : unmanaged, IRenderingBackend
        {
            backend.Start();
        }

        public static void PerformFinish<T>(ref T backend) where T : unmanaged, IRenderingBackend
        {
            backend.Finish();
        }

        public static Create.Output Create<T>(ref T backend, in Create.Input input) where T : unmanaged, IRenderingBackend
        {
            (MemoryAddress machine, MemoryAddress instance) = backend.Create(input.destination, input.ExtensionNames);
            return new(machine, instance);
        }

        public static void Dispose<T>(ref T backend, in Dispose.Input input) where T : unmanaged, IRenderingBackend
        {
            backend.Dispose(input.machine, input.instance);
        }

        public static void SurfaceCreated<T>(ref T backend, in MemoryAddress machine, MemoryAddress surface) where T : unmanaged, IRenderingBackend
        {
            backend.SurfaceCreated(machine, surface);
        }

        public static StatusCode BeginRender<T>(ref T backend, in MemoryAddress machine, in Vector4 clearColor) where T : unmanaged, IRenderingBackend
        {
            return backend.BeginRender(machine, clearColor);
        }

        public static void Render<T>(ref T backend, in Render.Input input) where T : unmanaged, IRenderingBackend
        {
            backend.Render(input.machine, input.renderGroup, input.Entities);
        }

        public static void EndRender<T>(ref T backend, in MemoryAddress machine) where T : unmanaged, IRenderingBackend
        {
            backend.EndRender(machine);
        }
    }
}
