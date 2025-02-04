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
        private readonly Operation addMissingComponents;

        private ClampNestedScissorViews(Array<Vector4> scissors, Array<bool> hasScissor, List<List<uint>> sortedEntities, Array<uint> parentEntities, Operation addMissingComponents)
        {
            this.scissors = scissors;
            this.hasScissor = hasScissor;
            this.sortedEntities = sortedEntities;
            this.parentEntities = parentEntities;
            this.addMissingComponents = addMissingComponents;
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

                addMissingComponents.Dispose();
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

            //add missing components
            Schema schema = world.Schema;
            ComponentQuery<RendererScissor> withoutWorldScissorQuery = new(world);
            withoutWorldScissorQuery.ExcludeComponent<WorldRendererScissor>();
            foreach (var r in withoutWorldScissorQuery)
            {
                addMissingComponents.SelectEntity(r.entity);
            }

            if (addMissingComponents.Count > 0)
            {
                addMissingComponents.AddComponent<WorldRendererScissor>(schema);
                world.Perform(addMissingComponents);
                addMissingComponents.Clear();
            }

            //gather values for later
            foreach (var child in world.Entities)
            {
                uint parent = world.GetParent(child);
                parentEntities[child] = parent;
            }

            ComponentQuery<RendererScissor> scissorQuery = new(world);
            foreach (var r in scissorQuery)
            {
                uint entity = r.entity;
                scissors[entity] = r.component1.value;
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
            ComponentQuery<WorldRendererScissor> worldScissorQuery = new(world);
            foreach (var r in worldScissorQuery)
            {
                ref WorldRendererScissor scissor = ref r.component1;
                scissor.value = scissors[r.entity];
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