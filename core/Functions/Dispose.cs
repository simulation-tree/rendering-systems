using Unmanaged;

namespace Rendering.Functions
{
    public unsafe readonly struct Dispose
    {
#if NET
        private readonly delegate* unmanaged<Allocation, Allocation, void> function;

        public Dispose(delegate* unmanaged<Allocation, Allocation, void> function)
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

        public readonly void Invoke(Allocation backend, Allocation renderer)
        {
            function(backend, renderer);
        }
    }
}