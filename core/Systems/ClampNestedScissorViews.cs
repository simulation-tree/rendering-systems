using Collections.Generic;
using Rendering.Components;
using Simulation;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Unmanaged;
using Worlds;

namespace Rendering.Systems
{
    public readonly partial struct ClampNestedScissorViews : ISystem
    {
        private readonly Array<Vector4> scissors;
        private readonly Array<bool> hasScissor;
        private readonly List<List<uint>> sortedEntities;
        private readonly Array<uint> parentEntities;
        private readonly Operation operation;

        public ClampNestedScissorViews()
        {
            this.scissors = new(4);
            this.hasScissor = new(4);
            this.sortedEntities = new(4);
            this.parentEntities = new(4);
            this.operation = new();
        }

        public readonly void Dispose()
        {
            foreach (List<uint> entities in sortedEntities)
            {
                entities.Dispose();
            }

            operation.Dispose();
            parentEntities.Dispose();
            sortedEntities.Dispose();
            hasScissor.Dispose();
            scissors.Dispose();
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            //prepare buffers
            int capacity = (world.MaxEntityValue + 1).GetNextPowerOf2();
            if (scissors.Length < capacity)
            {
                scissors.Length = capacity;
                hasScissor.Length = capacity;
                parentEntities.Length = capacity;
            }

            scissors.Clear();
            hasScissor.Clear();
            parentEntities.Clear();

            foreach (List<uint> entities in sortedEntities)
            {
                entities.Clear();
            }

            //add missing world scissor component
            int scissorComponent = world.Schema.GetComponentType<RendererScissor>();
            int worldScissorComponent = world.Schema.GetComponentType<WorldRendererScissor>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(scissorComponent) && !definition.ContainsComponent(worldScissorComponent))
                {
                    operation.SelectEntities(chunk.Entities);
                }
            }

            if (operation.Count > 0)
            {
                operation.AddComponentType<WorldRendererScissor>();
                operation.Perform(world);
                operation.Reset();
            }

            //gather values for later
            foreach (uint child in world.Entities)
            {
                uint parent = world.GetParent(child);
                parentEntities[(int)child] = parent;
            }

            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(scissorComponent))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<RendererScissor> components = chunk.GetComponents<RendererScissor>(scissorComponent);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        scissors[(int)entity] = components[i].value;
                        hasScissor[(int)entity] = true;
                        uint parent = parentEntities[(int)entity];
                        int depth = 0;
                        while (parent != default)
                        {
                            depth++;
                            parent = world.GetParent(parent);
                        }

                        while (sortedEntities.Count <= depth)
                        {
                            sortedEntities.Add(new());
                        }

                        sortedEntities[depth].Add(entity);
                    }
                }
            }

            //do the thing
            foreach (List<uint> entities in sortedEntities)
            {
                foreach (uint entity in entities)
                {
                    uint parent = parentEntities[(int)entity];
                    Vector4 parentScissor = default;
                    bool foundParentScissor = false;
                    while (parent != default)
                    {
                        if (hasScissor[(int)parent])
                        {
                            parentScissor = scissors[(int)parent];
                            foundParentScissor = true;
                            break;
                        }
                        else
                        {
                            parent = parentEntities[(int)parent];
                        }
                    }

                    if (foundParentScissor)
                    {
                        float parentMinX = parentScissor.X;
                        float parentMinY = parentScissor.Y;
                        float parentMaxX = parentScissor.X + parentScissor.Z;
                        float parentMaxY = parentScissor.Y + parentScissor.W;

                        ref Vector4 scissor = ref scissors[(int)entity];
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
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.ContainsComponent(worldScissorComponent))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<WorldRendererScissor> components = chunk.GetComponents<WorldRendererScissor>(worldScissorComponent);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        components[i].value = scissors[(int)entity];
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