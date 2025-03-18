using Collections.Generic;
using Simulation;
using System;
using System.Numerics;
using Unmanaged;

namespace Rendering.Systems.Tests
{
    public readonly partial struct TestRendererBackend : IRenderingBackend
    {
        public static bool initialized;
        public static readonly System.Collections.Generic.List<MemoryAddress> renderingMachines = new();

        readonly ASCIIText256 IRenderingBackend.Label => "test";

        void IRenderingBackend.Start()
        {
            initialized = true;
        }

        void IRenderingBackend.Finish()
        {
            initialized = false;
        }

        (MemoryAddress renderer, MemoryAddress instance) IRenderingBackend.Create(in Destination destination, in ReadOnlySpan<ASCIIText256> extensionNames)
        {
            MemoryAddress renderer = MemoryAddress.AllocateValue(new TestRenderer(destination, extensionNames));
            renderingMachines.Add(renderer);
            return (renderer, renderer);
        }

        void IRenderingBackend.Dispose(in MemoryAddress renderer)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.Dispose();
            renderingMachines.Remove(renderer);
            renderer.Dispose();
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
            testRenderer.material = default;
            testRenderer.mesh = default;
            testRenderer.vertexShader = default;
            testRenderer.fragmentShader = default;
            return StatusCode.Continue;
        }

        void IRenderingBackend.Render(in MemoryAddress renderer, in ReadOnlySpan<uint> entities, in MaterialData material, in MeshData mesh, in VertexShaderData vertexShader, in FragmentShaderData fragmentShader)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.entities.AddRange(entities);
            testRenderer.material = material;
            testRenderer.mesh = mesh;
            testRenderer.vertexShader = vertexShader;
            testRenderer.fragmentShader = fragmentShader;
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
        public readonly Array<ASCIIText256> extensionNames;
        public readonly List<uint> entities;
        public Vector4 clearColor;
        public MaterialData material;
        public MeshData mesh;
        public VertexShaderData vertexShader;
        public FragmentShaderData fragmentShader;
        public MemoryAddress surface;
        public bool finishedRendering;

        public TestRenderer(Destination destination, ReadOnlySpan<ASCIIText256> extensionNames)
        {
            this.destination = destination;
            this.extensionNames = new(extensionNames);
            entities = new();
        }

        public void Dispose()
        {
            entities.Dispose();
            extensionNames.Dispose();
        }
    }
}
