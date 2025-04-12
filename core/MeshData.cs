using Meshes;
using System;
using Worlds;

namespace Rendering
{
    public readonly struct MeshData : IEquatable<MeshData>
    {
        public readonly uint entity;
        public readonly uint version;

        public MeshData(uint entity, uint version)
        {
            this.entity = entity;
            this.version = version;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is MeshData data && Equals(data);
        }

        public readonly bool Equals(MeshData other)
        {
            return entity == other.entity && version == other.version;
        }

        public readonly Mesh Get(World world)
        {
            return new Entity(world, entity).As<Mesh>();
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(entity, version);
        }

        public static bool operator ==(MeshData left, MeshData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MeshData left, MeshData right)
        {
            return !(left == right);
        }
    }
}