using System;
using Unmanaged;

namespace Rendering.Functions
{
    /// <summary>
    /// Initializes the rendering backend system.
    /// <para>
    /// Balanced with the <see cref="Finalize"/> function.
    /// </para>
    /// </summary>
    public unsafe readonly struct Initialize : IEquatable<Initialize>
    {
#if NET
        private readonly delegate* unmanaged<MemoryAddress, void> function;

        public Initialize(delegate* unmanaged<MemoryAddress, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Allocation, void> function;

        public Initialize(delegate*<Allocation, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(MemoryAddress renderer)
        {
            function(renderer);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Initialize initialize && Equals(initialize);
        }

        public readonly bool Equals(Initialize other)
        {
            return (nint)function == (nint)other.function;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public static bool operator ==(Initialize left, Initialize right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Initialize left, Initialize right)
        {
            return !(left == right);
        }
    }
}