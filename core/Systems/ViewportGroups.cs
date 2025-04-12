using Collections.Generic;
using System;

namespace Rendering.Systems
{
    internal readonly struct ViewportGroups : IDisposable
    {
        private readonly Array<ViewportGroup> groups;

        public readonly int Capacity => groups.Length;

        public ViewportGroups()
        {
            groups = new();
        }

        public readonly Span<ViewportGroup> AsSpan()
        {
            return groups.AsSpan();
        }

        public readonly void Dispose()
        {
            foreach (ViewportGroup group in groups)
            {
                group.Dispose();
            }

            groups.Dispose();
        }

        public readonly void EnsureCapacity(int newCapacity)
        {
            if (groups.Length < newCapacity)
            {
                int oldCount = groups.Length;
                groups.Length = newCapacity;
                for (int i = oldCount; i < newCapacity; i++)
                {
                    groups[i] = new();
                }
            }
        }
    }
}