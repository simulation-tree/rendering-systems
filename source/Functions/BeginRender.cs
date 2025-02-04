using Simulation;
using System.Numerics;
using Unmanaged;

namespace Rendering.Functions
{
    public unsafe readonly struct BeginRender
    {
#if NET
        private readonly delegate* unmanaged<Allocation, Allocation, Vector4, StatusCode> function;

        public BeginRender(delegate* unmanaged<Allocation, Allocation, Vector4, StatusCode> function)
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

        public readonly StatusCode Invoke(Allocation backend, Allocation renderer, Vector4 color)
        {
            return function(backend, renderer, color);
        }
    }
}