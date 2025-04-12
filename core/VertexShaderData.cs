using Shaders;
using System;
using Worlds;

namespace Rendering
{
    public readonly struct VertexShaderData : IEquatable<VertexShaderData>
    {
        public readonly uint entity;
        public readonly uint version;

        public VertexShaderData(uint entity, uint version)
        {
            this.entity = entity;
            this.version = version;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is VertexShaderData data && Equals(data);
        }

        public readonly bool Equals(VertexShaderData other)
        {
            return entity == other.entity && version == other.version;
        }

        public readonly Shader Get(World world)
        {
            return new Entity(world, entity).As<Shader>();
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(entity, version);
        }

        public static bool operator ==(VertexShaderData left, VertexShaderData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexShaderData left, VertexShaderData right)
        {
            return !(left == right);
        }
    }
}