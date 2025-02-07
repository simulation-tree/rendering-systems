using Collections;
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

        private ClampNestedScissorViews(Array<Vector4> scissors, Array<bool> hasScissor, List<List<uint>> sortedEntities, Array<uint> parentEntities, Operation addMissingComponents)
        {
            this.scissors = scissors;
            this.hasScissor = hasScissor;
            this.sortedEntities = sortedEntities;
            this.parentEntities = parentEntities;
            this.operation = addMissingComponents;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                Array<Vector4> scissor = new();
                Array<bool> hasScissor = new();
                List<List<uint>> sortedEntities = new();
                Array<uint> parentEntities = new();
                Operation addMissingComponents = new();
                systemContainer.Write(new ClampNestedScissorViews(scissor, hasScissor, sortedEntities, parentEntities, addMissingComponents));
            }
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
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
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            //prepare buffers
            uint capacity = Allocations.GetNextPowerOf2(world.MaxEntityValue + 1);
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
            ComponentType scissorComponent = world.Schema.GetComponent<RendererScissor>();
            ComponentType worldScissorComponent = world.Schema.GetComponent<WorldRendererScissor>();
            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.Contains(scissorComponent) && !definition.Contains(worldScissorComponent))
                {
                    operation.SelectEntities(chunk.Entities);
                }
            }

            if (operation.Count > 0)
            {
                operation.AddComponent<WorldRendererScissor>();
                operation.Perform(world);
                operation.Clear();
            }

            //gather values for later
            foreach (uint child in world.Entities)
            {
                uint parent = world.GetParent(child);
                parentEntities[child] = parent;
            }

            foreach (Chunk chunk in world.Chunks)
            {
                Definition definition = chunk.Definition;
                if (definition.Contains(scissorComponent))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<RendererScissor> components = chunk.GetComponents<RendererScissor>(scissorComponent);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        scissors[entity] = components[i].value;
                        hasScissor[entity] = true;
                        uint parent = parentEntities[entity];
                        uint depth = 0;
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
                    uint parent = parentEntities[entity];
                    Vector4 parentScissor = default;
                    bool foundParentScissor = false;
                    while (parent != default)
                    {
                        if (hasScissor[parent])
                        {
                            parentScissor = scissors[parent];
                            foundParentScissor = true;
                            break;
                        }
                        else
                        {
                            parent = parentEntities[parent];
                        }
                    }

                    if (foundParentScissor)
                    {
                        float parentMinX = parentScissor.X;
                        float parentMinY = parentScissor.Y;
                        float parentMaxX = parentScissor.X + parentScissor.Z;
                        float parentMaxY = parentScissor.Y + parentScissor.W;

                        ref Vector4 scissor = ref scissors[entity];
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
                if (definition.Contains(worldScissorComponent))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<WorldRendererScissor> components = chunk.GetComponents<WorldRendererScissor>(worldScissorComponent);
                    for (uint i = 0; i < entities.Length; i++)
                    {
                        uint entity = entities[i];
                        components[i].value = scissors[entity];
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