using Materials.Components;
using Meshes.Components;
using Rendering.Components;
using Shaders.Components;
using System;

namespace Rendering
{
    internal struct EntityComponents : IEquatable<EntityComponents>
    {
        public Flags flags;
        public IsRenderer renderer;
        public IsMaterial material;
        public IsShader shader;
        public IsMesh mesh;

        public readonly bool ContainsRenderer => (flags & Flags.ContainsRenderer) != 0;
        public readonly bool ContainsMaterial => (flags & Flags.ContainsMaterial) != 0;
        public readonly bool ContainsShader => (flags & Flags.ContainsShader) != 0;
        public readonly bool ContainsMesh => (flags & Flags.ContainsMesh) != 0;

        public void SetRenderer(IsRenderer renderer)
        {
            flags |= Flags.ContainsRenderer;
            this.renderer = renderer;
        }

        public void SetMaterial(IsMaterial material)
        {
            flags |= Flags.ContainsMaterial;
            this.material = material;
        }

        public void SetShader(IsShader shader)
        {
            flags |= Flags.ContainsShader;
            this.shader = shader;
        }

        public void SetMesh(IsMesh mesh)
        {
            flags |= Flags.ContainsMesh;
            this.mesh = mesh;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is EntityComponents components && Equals(components);
        }

        public readonly bool Equals(EntityComponents other)
        {
            return flags == other.flags && renderer.Equals(other.renderer) && material.Equals(other.material) && shader.Equals(other.shader) && mesh.Equals(other.mesh);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(flags, renderer, material, shader, mesh);
        }

        public static bool operator ==(EntityComponents left, EntityComponents right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityComponents left, EntityComponents right)
        {
            return !(left == right);
        }

        [Flags]
        public enum Flags
        {
            None = 0,
            ContainsRenderer = 1,
            ContainsMaterial = 2,
            ContainsShader = 4,
            ContainsMesh = 8
        }
    }
}