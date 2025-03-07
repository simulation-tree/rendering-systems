using Simulation;
using Unmanaged;

namespace Rendering.Functions
{
    public unsafe readonly struct EndRender
    {
#if NET
        private readonly delegate* unmanaged<MemoryAddress, MemoryAddress, void> function;

        public EndRender(delegate* unmanaged<MemoryAddress, MemoryAddress, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Allocation, Allocation, void> function;

        public EndRender(delegate*<Allocation, Allocation, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(MemoryAddress backend, MemoryAddress renderer)
        {
            function(backend, renderer);
        }
    }
}