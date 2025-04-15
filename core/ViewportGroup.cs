using Collections.Generic;
using System;

namespace Rendering.Systems
{
    internal struct ViewportGroup : IDisposable
    {
        public sbyte order;
        public readonly Dictionary<MaterialData, List<RenderEntity>> map;

        public ViewportGroup()
        {
            map = new();
        }

        public readonly void Dispose()
        {
            foreach (List<RenderEntity> renderEntities in map.Values)
            {
                renderEntities.Dispose();
            }

            map.Dispose();
        }
    }
}