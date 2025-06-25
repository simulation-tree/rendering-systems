using Collections.Generic;
using Rendering.Components;
using Rendering.Messages;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Unmanaged;
using Worlds;

namespace Rendering.Systems
{
    public partial class ClampNestedScissorViews : SystemBase, IListener<RenderUpdate>
    {
        private readonly World world;
        private readonly Array<Vector4> scissors;
        private readonly Array<bool> hasScissor;
        private readonly List<List<uint>> sortedEntities;
        private readonly Operation operation;
        private readonly int localScissorType;
        private readonly int worldScissorType;

        public ClampNestedScissorViews(Simulator simulator, World world) : base(simulator)
        {
            this.world = world;
            scissors = new(4);
            hasScissor = new(4);
            sortedEntities = new(4);
            operation = new(world);

            Schema schema = world.Schema;
            localScissorType = schema.GetComponentType<RendererScissor>();
            worldScissorType = schema.GetComponentType<WorldRendererScissor>();
        }

        public override void Dispose()
        {
            foreach (List<uint> entities in sortedEntities)
            {
                entities.Dispose();
            }

            operation.Dispose();
            sortedEntities.Dispose();
            hasScissor.Dispose();
            scissors.Dispose();
        }

        void IListener<RenderUpdate>.Receive(ref RenderUpdate message)
        {
            //prepare buffers
            int capacity = (world.MaxEntityValue + 1).GetNextPowerOf2();
            if (scissors.Length < capacity)
            {
                scissors.Length = capacity;
                hasScissor.Length = capacity;
            }

            scissors.Clear();
            hasScissor.Clear();

            int maxDepth = world.MaxDepth;
            if (sortedEntities.Count <= maxDepth)
            {
                int toAdd = maxDepth - sortedEntities.Count;
                for (int i = 0; i <= toAdd; i++)
                {
                    sortedEntities.Add(new(32));
                }
            }

            Span<Vector4> scissorsSpan = scissors.AsSpan();
            Span<bool> hasScissorSpan = hasScissor.AsSpan();
            Span<List<uint>> sortedEntitiesSpan = sortedEntities.AsSpan();
            for (int d = 0; d < sortedEntitiesSpan.Length; d++)
            {
                sortedEntitiesSpan[d].Clear();
            }

            //add missing world scissor component
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.Count > 0)
                {
                    Definition definition = chunk.Definition;
                    if (definition.ContainsComponent(localScissorType) && !definition.ContainsComponent(worldScissorType))
                    {
                        operation.AppendMultipleEntitiesToSelection(chunk.Entities);
                    }
                }
            }

            if (operation.Count > 0)
            {
                operation.AddComponentType(worldScissorType);
                operation.Perform();
                operation.Reset();
            }

            //gather values for later
            chunks = world.Chunks;
            ReadOnlySpan<Slot> slots = world.Slots;
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.Definition.ContainsComponent(localScissorType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<RendererScissor> components = chunk.GetComponents<RendererScissor>(localScissorType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        scissorsSpan[(int)entity] = components[i].value;
                        hasScissorSpan[(int)entity] = true;
                        sortedEntitiesSpan[slots[(int)entity].depth].Add(entity);
                    }
                }
            }

            //do the thing
            for (int d = 0; d < sortedEntitiesSpan.Length; d++)
            {
                Span<uint> entities = sortedEntitiesSpan[d].AsSpan();
                for (int i = 0; i < entities.Length; i++)
                {
                    uint entity = entities[i];
                    uint parent = slots[(int)entity].parent;
                    Vector4 parentScissor = default;
                    bool foundParentScissor = false;
                    while (parent != default)
                    {
                        if (hasScissorSpan[(int)parent])
                        {
                            parentScissor = scissorsSpan[(int)parent];
                            foundParentScissor = true;
                            break;
                        }
                        else
                        {
                            parent = slots[(int)parent].parent;
                        }
                    }

                    if (foundParentScissor)
                    {
                        float parentMinX = parentScissor.X;
                        float parentMinY = parentScissor.Y;
                        float parentMaxX = parentScissor.X + parentScissor.Z;
                        float parentMaxY = parentScissor.Y + parentScissor.W;

                        ref Vector4 scissor = ref scissorsSpan[(int)entity];
                        float minX = scissor.X;
                        float minY = scissor.Y;
                        float maxX = scissor.X + scissor.Z;
                        float maxY = scissor.Y + scissor.W;

                        //make sure the child scissor view is contained inside the parent's
                        minX = Clamp(minX, parentMinX, parentMaxX);
                        minY = Clamp(minY, parentMinY, parentMaxY);
                        maxX = Clamp(maxX, parentMinX, parentMaxX);
                        maxY = Clamp(maxY, parentMinY, parentMaxY);

                        scissor.X = minX;
                        scissor.Y = minY;
                        scissor.Z = maxX - minX;
                        scissor.W = maxY - minY;
                    }
                }
            }

            //apply values
            for (int c = 0; c < chunks.Length; c++)
            {
                Chunk chunk = chunks[c];
                if (chunk.Definition.ContainsComponent(worldScissorType))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<WorldRendererScissor> components = chunk.GetComponents<WorldRendererScissor>(worldScissorType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        components[i].value = scissorsSpan[(int)entities[i]];
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }
            else
            {
                return value;
            }
        }
    }
}