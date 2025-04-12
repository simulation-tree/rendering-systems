using Materials;
using System;
using Worlds;

namespace Rendering
{
    public readonly struct MaterialData : IEquatable<MaterialData>
    {
        public readonly uint entity;
        public readonly uint version;

        public MaterialData(uint entity, uint version)
        {
            this.entity = entity;
            this.version = version;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is MaterialData data && Equals(data);
        }

        public readonly bool Equals(MaterialData other)
        {
            return entity == other.entity && version == other.version;
        }

        public readonly Material Get(World world)
        {
            return new Entity(world, entity).As<Material>();
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(entity, version);
        }

        public static bool operator ==(MaterialData left, MaterialData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MaterialData left, MaterialData right)
        {
            return !(left == right);
        }
    }
}