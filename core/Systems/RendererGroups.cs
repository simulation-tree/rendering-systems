using Collections.Generic;
using System;

namespace Rendering.Systems
{
    internal readonly struct RendererGroups : IDisposable
    {
        private readonly List<List<uint>> lists;
        private readonly List<RendererCombination> combinations;
        private readonly Stack<int> free;
        private readonly Dictionary<RendererCombination, int> listIndices;

        public readonly ReadOnlySpan<RendererCombination> Combinations => combinations.AsSpan();

        public RendererGroups()
        {
            lists = new();
            combinations = new();
            free = new();
            listIndices = new();
        }

        public readonly void Dispose()
        {
            listIndices.Dispose();
            free.Dispose();
            combinations.Dispose();

            foreach (List<uint> list in lists)
            {
                list.Dispose();
            }

            lists.Dispose();
        }

        public readonly void Clear()
        {
            for (int i = 0; i < lists.Count; i++)
            {
                if (free.TryPush(i))
                {
                    lists[i].Clear();
                }
            }

            combinations.Clear();
            listIndices.Clear();
        }

        public readonly void Add(RendererCombination combination, uint rendererEntity)
        {
            List<uint> list;
            if (listIndices.TryGetValue(combination, out int index))
            {
                list = lists[index];
            }
            else
            {
                if (free.TryPop(out index))
                {
                    list = lists[index];
                }
                else
                {
                    index = lists.Count;
                    list = new();
                    lists.Add(list);
                }

                combinations.Add(combination);
                listIndices.Add(combination, index);
            }

            list.Add(rendererEntity);
        }

        public readonly ReadOnlySpan<uint> GetEntities(RendererCombination combination)
        {
            int index = listIndices[combination];
            return lists[index].AsSpan();
        }
    }
}