using Simulation;
using System;
using System.Numerics;
using Unmanaged;

namespace Rendering
{
    public static class RenderingBackendExtensions
    {
        public static void Initialize<T>(this ref T backend) where T : unmanaged, IRenderingBackend
        {
            backend.Initialize();
        }

        public static void PerformFinalize<T>(this ref T backend) where T : unmanaged, IRenderingBackend
        {
            backend.Finalize();
        }

        public static (MemoryAddress renderer, MemoryAddress instance) Create<T>(this ref T backend, in Destination destination, in Span<ASCIIText256> extensionNames) where T : unmanaged, IRenderingBackend
        {
            return backend.Create(destination, extensionNames);
        }

        public static void Dispose<T>(this ref T backend, in MemoryAddress renderer) where T : unmanaged, IRenderingBackend
        {
            backend.Dispose(renderer);
        }

        public static void SurfaceCreated<T>(this ref T backend, in MemoryAddress renderer, MemoryAddress surface) where T : unmanaged, IRenderingBackend
        {
            backend.SurfaceCreated(renderer, surface);
        }

        public static StatusCode BeginRender<T>(this ref T backend, in MemoryAddress renderer, in Vector4 clearColor) where T : unmanaged, IRenderingBackend
        {
            return backend.BeginRender(renderer, clearColor);
        }

        public static void Render<T>(this ref T backend, in MemoryAddress renderer, in Span<uint> entities, in MaterialData material, in MeshData mesh, in VertexShaderData vertexShader, in FragmentShaderData fragmentShader) where T : unmanaged, IRenderingBackend
        {
            backend.Render(renderer, entities, material, mesh, vertexShader, fragmentShader);
        }

        public static void EndRender<T>(this ref T backend, in MemoryAddress renderer) where T : unmanaged, IRenderingBackend
        {
            backend.EndRender(renderer);
        }
    }
}
