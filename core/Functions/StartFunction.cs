using System;
using Unmanaged;

namespace Rendering.Functions
{
    /// <summary>
    /// Initializes the rendering backend system.
    /// <para>
    /// Balanced with the <see cref="FinishFunction"/> function.
    /// </para>
    /// </summary>
    public unsafe readonly struct StartFunction : IEquatable<StartFunction>
    {
#if NET
        private readonly delegate* unmanaged<MemoryAddress, void> function;

        public StartFunction(delegate* unmanaged<MemoryAddress, void> function)
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
            return obj is StartFunction initialize && Equals(initialize);
        }

        public readonly bool Equals(StartFunction other)
        {
            return (nint)function == (nint)other.function;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public static bool operator ==(StartFunction left, StartFunction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StartFunction left, StartFunction right)
        {
            return !(left == right);
        }
    }
}