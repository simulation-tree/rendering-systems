using Collections.Generic;
using Materials.Components;
using System;

namespace Rendering.Systems
{
    internal struct ViewportGroup : IDisposable
    {
        private uint entity;
        private readonly List<(uint rendererEntity, uint materialEntity, IsMaterial material)> rendererEntities;

        public readonly uint Entity => entity;
        public readonly Span<(uint rendererEntity, uint materialEntity, IsMaterial material)> Renderers => rendererEntities.AsSpan();

        /// <summary>
        /// Creates a new viewport group containing renderer entities.
        /// </summary>
        public ViewportGroup()
        {
            rendererEntities = new(256);
        }

        public void Initialize(uint entity)
        {
            this.entity = entity;
            rendererEntities.Clear();
        }

        public readonly void Add(uint rendererEntity, uint materialEntity, IsMaterial material)
        {
            rendererEntities.Add((rendererEntity, materialEntity, material));
        }

        public readonly void Dispose()
        {
            rendererEntities.Dispose();
        }
    }
}