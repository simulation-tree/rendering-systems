using Collections.Generic;
using Simulation;
using System;
using System.Numerics;
using Unmanaged;

namespace Rendering.Systems.Tests
{
    public readonly partial struct TestRendererBackend : IRenderingBackend
    {
        public static MemoryAddress library;
        public static bool initialized;
        public static readonly System.Collections.Generic.List<MemoryAddress> renderingMachines = new();

        readonly ASCIIText256 IRenderingBackend.Label => "test";

        void IRenderingBackend.Start()
        {
            initialized = true;
            library = MemoryAddress.AllocateValue(32);
        }

        void IRenderingBackend.Finish()
        {
            library.Dispose();
            initialized = false;
        }

        (MemoryAddress machine, MemoryAddress instance) IRenderingBackend.Create(in Destination destination, in ReadOnlySpan<DestinationExtension> extensionNames)
        {
            MemoryAddress instance = MemoryAddress.AllocateValue(1000);
            TestRenderer machine = new(destination, extensionNames, instance);
            MemoryAddress machineAddress = MemoryAddress.AllocateValue(machine);
            renderingMachines.Add(machineAddress);
            return (machineAddress, instance);
        }

        void IRenderingBackend.Dispose(in MemoryAddress renderer, in MemoryAddress instance)
        {
            renderingMachines.Remove(renderer);
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.Dispose();
            renderer.Dispose();
            instance.Dispose();
        }

        void IRenderingBackend.SurfaceCreated(in MemoryAddress renderer, MemoryAddress surface)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.surface = surface;
        }

        StatusCode IRenderingBackend.BeginRender(in MemoryAddress renderer, in Vector4 clearColor)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.clearColor = clearColor;
            testRenderer.entities.Clear();
            testRenderer.renderGroup = default;
            return StatusCode.Continue;
        }

        void IRenderingBackend.Render(in MemoryAddress renderer, in sbyte renderGroup, in ReadOnlySpan<RenderEntity> entities)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.renderGroup = renderGroup;
            testRenderer.entities.AddRange(entities);
        }

        void IRenderingBackend.EndRender(in MemoryAddress renderer)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.finishedRendering = true;
        }
    }

    public struct TestRenderer : IDisposable
    {
        public readonly Destination destination;
        public readonly Array<DestinationExtension> extensionNames;
        public readonly List<RenderEntity> entities;
        public readonly MemoryAddress instance;
        public sbyte renderGroup;
        public Vector4 clearColor;
        public MemoryAddress surface;
        public bool finishedRendering;

        public TestRenderer(Destination destination, ReadOnlySpan<DestinationExtension> extensionNames, MemoryAddress instance)
        {
            this.destination = destination;
            this.extensionNames = new(extensionNames);
            this.instance = instance;
            entities = new();
        }

        public void Dispose()
        {
            entities.Dispose();
            extensionNames.Dispose();
        }
    }
}
