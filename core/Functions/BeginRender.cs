using Simulation;
using System.Numerics;
using Unmanaged;

namespace Rendering.Functions
{
    public unsafe readonly struct BeginRender
    {
#if NET
        private readonly delegate* unmanaged<MemoryAddress, MemoryAddress, Vector4, StatusCode> function;

        public BeginRender(delegate* unmanaged<MemoryAddress, MemoryAddress, Vector4, StatusCode> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Allocation, Allocation, Vector4, StatusCode> function;

        public BeginRenderFunction(delegate*<Allocation, Allocation, Vector4, StatusCode> function)
        {
            this.function = function;
        }
#endif

        public readonly StatusCode Invoke(MemoryAddress backend, MemoryAddress renderer, Vector4 color)
        {
            return function(backend, renderer, color);
        }
    }
}