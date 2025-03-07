using Unmanaged;

namespace Rendering.Functions
{
    public unsafe readonly struct Dispose
    {
#if NET
        private readonly delegate* unmanaged<MemoryAddress, MemoryAddress, void> function;

        public Dispose(delegate* unmanaged<MemoryAddress, MemoryAddress, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Allocation, Allocation, void> function;

        public Dispose(delegate*<Allocation, Allocation, void> function)
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