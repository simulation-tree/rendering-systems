using Materials;
using Meshes;
using System;

namespace Rendering
{
    public readonly struct RendererKey : IEquatable<RendererKey>
    {
        public readonly ulong value;

        public RendererKey(uint materialEntity, uint meshEntity)
        {
            value = ((ulong)materialEntity << 32) | meshEntity;
        }

        public RendererKey(Material material, Mesh mesh)
        {
            value = ((ulong)material.value << 32) | mesh.value;
        }

        public readonly bool Equals(RendererKey other)
        {
            return value == other.value;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is RendererKey key && Equals(key);
        }

        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator ==(RendererKey left, RendererKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RendererKey left, RendererKey right)
        {
            return !left.Equals(right);
        }
    }
}