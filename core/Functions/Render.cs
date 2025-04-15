using System;
using Unmanaged;

namespace Rendering.Functions
{
    /// <summary>
    /// Renders a batch of entities using the same material, mesh and camera combination.
    /// </summary>
    public unsafe readonly struct Render
    {
#if NET
        private readonly delegate* unmanaged<Input, void> function;

        public Render(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, void> function;

        public Render(delegate*<Input, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(MemoryAddress backend, MemoryAddress machine, uint materialEntity, ushort materialVersion, ReadOnlySpan<RenderEntity> entities)
        {
            function(new(backend, machine, materialEntity, materialVersion, entities));
        }

        public readonly struct Input
        {
            public readonly MemoryAddress backend;
            public readonly MemoryAddress machine;
            public readonly uint materialEntity;
            public readonly ushort materialVersion;

            private readonly RenderEntity* entities;
            private readonly int count;

            /// <summary>
            /// All entities containing the same filter, callback and identifier combinations.
            /// </summary>
            public readonly ReadOnlySpan<RenderEntity> Entities => new(entities, count);

            public Input(MemoryAddress backend, MemoryAddress machine, uint materialEntity, ushort materialVersion, ReadOnlySpan<RenderEntity> entities)
            {
                this.backend = backend;
                this.machine = machine;
                this.materialEntity = materialEntity;
                this.materialVersion = materialVersion;
                this.entities = entities.GetPointer();
                count = entities.Length;
            }
        }
    }
}