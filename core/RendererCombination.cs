using System;

namespace Rendering
{
    public struct RendererCombination : IEquatable<RendererCombination>
    {
        public uint materialEntity;
        public uint meshEntity;
        public uint vertexShaderEntity;
        public uint fragmentShaderEntity;

        public readonly ulong Key => ((ulong)materialEntity << 32) | meshEntity;

        public RendererCombination(uint materialEntity, uint meshEntity, uint vertexShaderEntity, uint fragmentShaderEntity)
        {
            this.materialEntity = materialEntity;
            this.meshEntity = meshEntity;
            this.vertexShaderEntity = vertexShaderEntity;
            this.fragmentShaderEntity = fragmentShaderEntity;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is RendererCombination combination && Equals(combination);
        }

        public readonly bool Equals(RendererCombination other)
        {
            return materialEntity == other.materialEntity && meshEntity == other.meshEntity && vertexShaderEntity == other.vertexShaderEntity && fragmentShaderEntity == other.fragmentShaderEntity;
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (int)materialEntity;
                hash = hash * 23 + (int)meshEntity;
                hash = hash * 23 + (int)vertexShaderEntity;
                hash = hash * 23 + (int)fragmentShaderEntity;
                return hash;
            }
        }

        public static bool operator ==(RendererCombination left, RendererCombination right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RendererCombination left, RendererCombination right)
        {
            return !(left == right);
        }
    }
}