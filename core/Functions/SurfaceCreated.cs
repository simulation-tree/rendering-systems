using Unmanaged;

namespace Rendering.Functions
{
    public unsafe readonly struct SurfaceCreated
    {
#if NET
        private readonly delegate* unmanaged<MemoryAddress, MemoryAddress, MemoryAddress, void> function;

        public SurfaceCreated(delegate* unmanaged<MemoryAddress, MemoryAddress, MemoryAddress, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Allocation, Allocation, Allocation, void> function;

        public SurfaceCreated(delegate*<Allocation, Allocation, Allocation, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(MemoryAddress backend, MemoryAddress renderer, MemoryAddress surface)
        {
            function(backend, renderer, surface);
        }
    }
}