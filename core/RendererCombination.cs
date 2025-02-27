using System;

namespace Rendering
{
    public struct RendererCombination : IEquatable<RendererCombination>
    {
        public uint material;
        public uint mesh;
        public uint vertexShader;
        public uint fragmentShader;

        public readonly ulong Key => ((ulong)material << 32) | mesh;

        public RendererCombination(uint material, uint mesh, uint vertexShader, uint fragmentShader)
        {
            this.material = material;
            this.mesh = mesh;
            this.vertexShader = vertexShader;
            this.fragmentShader = fragmentShader;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is RendererCombination combination && Equals(combination);
        }

        public readonly bool Equals(RendererCombination other)
        {
            return material == other.material && mesh == other.mesh && vertexShader == other.vertexShader && fragmentShader == other.fragmentShader;
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (int)material;
                hash = hash * 23 + (int)mesh;
                hash = hash * 23 + (int)vertexShader;
                hash = hash * 23 + (int)fragmentShader;
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