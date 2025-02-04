using Unmanaged;

namespace Rendering.Functions
{
    public unsafe readonly struct SurfaceCreated
    {
#if NET
        private readonly delegate* unmanaged<Allocation, Allocation, Allocation, void> function;

        public SurfaceCreated(delegate* unmanaged<Allocation, Allocation, Allocation, void> function)
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

        public readonly void Invoke(Allocation backend, Allocation renderer, Allocation surface)
        {
            function(backend, renderer, surface);
        }
    }
}