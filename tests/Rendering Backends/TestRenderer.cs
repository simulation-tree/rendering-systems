using Collections.Generic;
using System;
using System.Numerics;
using Unmanaged;

namespace Rendering.Systems.Tests
{
    public class TestRenderer : RenderingMachine
    {
        public readonly Array<DestinationExtension> extensionNames;
        public readonly List<RenderEntity> entities;
        public sbyte renderGroup;
        public Vector4 clearColor;
        public MemoryAddress surface;
        public bool finishedRendering;

        public TestRenderer(Destination destination, MemoryAddress instance, ReadOnlySpan<DestinationExtension> extensionNames) : base(destination, instance)
        {
            this.extensionNames = new(extensionNames);
            entities = new();
        }

        public override bool BeginRender(Vector4 clearColor)
        {
            return true;
        }

        public void Dispose()
        {
            entities.Dispose();
            extensionNames.Dispose();
        }

        public override void EndRender()
        {
        }

        public override void Render(sbyte renderGroup, ReadOnlySpan<RenderEntity> renderers)
        {
            this.renderGroup = renderGroup;
            entities.Clear();
            entities.AddRange(renderers);
        }

        public override void SurfaceCreated(MemoryAddress surface)
        {
            this.surface = surface;
        }
    }
}
