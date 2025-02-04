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

        public readonly (Allocation renderer, Allocation instance) Invoke(Allocation backend, Destination destination, USpan<FixedString> extensionNames)
        {
            Input input = new(backend, destination, extensionNames);
            Output result = function(input);
            return (result.renderer, result.instance);
        }

        public readonly struct Input
        {
            public readonly Allocation backend;
            public readonly Destination destination;

            private readonly FixedString* extensionNames;
            private readonly uint length;

            public readonly USpan<FixedString> ExtensionNames => new(extensionNames, length);

            public Input(Allocation backend, Destination destination, USpan<FixedString> extensionNames)
            {
                this.backend = backend;
                this.destination = destination;
                this.extensionNames = (FixedString*)extensionNames.Address;
                length = extensionNames.Length;
            }
        }

        public readonly struct Output
        {
            public readonly Allocation renderer;
            public readonly Allocation instance;

            public Output(Allocation renderer, Allocation instance)
            {
                this.renderer = renderer;
                this.instance = instance;
            }
        }
    }
}