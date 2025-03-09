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

        public readonly void Invoke(MemoryAddress backend, MemoryAddress renderer, Span<uint> entities, MaterialData material, MeshData mesh, VertexShaderData vertexShader, FragmentShaderData fragmentShader)
        {
            function(new(backend, renderer, entities, material, mesh, vertexShader, fragmentShader));
        }

        public readonly struct Input
        {
            public readonly MemoryAddress backend;
            public readonly MemoryAddress renderer;
            public readonly MaterialData material;
            public readonly MeshData mesh;
            public readonly VertexShaderData vertexShader;
            public readonly FragmentShaderData fragmentShader;

            private readonly uint* entities;
            private readonly int count;

            /// <summary>
            /// All entities containing the same filter, callback and identifier combinations.
            /// </summary>
            public readonly Span<uint> Entities => new(entities, count);

            public Input(MemoryAddress backend, MemoryAddress renderer, Span<uint> entities, MaterialData material, MeshData mesh, VertexShaderData vertexShader, FragmentShaderData fragmentShader)
            {
                this.backend = backend;
                this.renderer = renderer;
                this.material = material;
                this.mesh = mesh;
                this.vertexShader = vertexShader;
                this.fragmentShader = fragmentShader;
                this.entities = entities.GetPointer();
                count = entities.Length;
            }
        }
    }
}