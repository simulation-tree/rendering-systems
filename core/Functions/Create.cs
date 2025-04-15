using System;
using Unmanaged;

namespace Rendering.Functions
{
    /// <summary>
    /// Creates a system instance for an existing destination entity.
    /// </summary>
    public unsafe readonly struct Create
    {
#if NET
        private readonly delegate* unmanaged<Input, Output> function;

        public Create(delegate* unmanaged<Input, Output> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, CreateResult> function;

        public Create(delegate*<Input, CreateResult> function)
        {
            this.function = function;
        }
#endif

        public readonly (MemoryAddress machine, MemoryAddress instance) Invoke(MemoryAddress backend, Destination destination, ReadOnlySpan<DestinationExtension> extensionNames)
        {
            Input input = new(backend, destination, extensionNames);
            Output result = function(input);
            return (result.machine, result.instance);
        }

        public readonly struct Input
        {
            public readonly MemoryAddress backend;
            public readonly Destination destination;

            private readonly void* extensionNames;
            private readonly int length;

            public readonly ReadOnlySpan<DestinationExtension> ExtensionNames => new(extensionNames, length);

            public Input(MemoryAddress backend, Destination destination, ReadOnlySpan<DestinationExtension> extensionNames)
            {
                this.backend = backend;
                this.destination = destination;
                this.extensionNames = extensionNames.GetPointer();
                length = extensionNames.Length;
            }
        }

        public readonly struct Output
        {
            public readonly MemoryAddress machine;
            public readonly MemoryAddress instance;

            public Output(MemoryAddress machine, MemoryAddress instance)
            {
                this.machine = machine;
                this.instance = instance;
            }
        }
    }
}