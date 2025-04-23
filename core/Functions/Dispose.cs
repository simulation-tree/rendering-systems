using Unmanaged;

namespace Rendering.Functions
{
    public unsafe readonly struct Dispose
    {
#if NET
        private readonly delegate* unmanaged<Input, void> function;

        public Dispose(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, void> function;

        public Dispose(delegate*<Input, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(MemoryAddress backend, MemoryAddress machine, MemoryAddress instance)
        {
            function(new(backend, machine, instance));
        }

        public readonly struct Input
        {
            public readonly MemoryAddress backend;
            public readonly MemoryAddress machine;
            public readonly MemoryAddress instance;

            public Input(MemoryAddress backend, MemoryAddress machine, MemoryAddress instance)
            {
                this.backend = backend;
                this.machine = machine;
                this.instance = instance;
            }
        }
    }
}