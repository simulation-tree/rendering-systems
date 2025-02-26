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
        public static readonly System.Collections.Generic.List<Allocation> renderingMachines = new();

        readonly FixedString IRenderingBackend.Label => "test";

        void IRenderingBackend.Initialize()
        {
            initialized = true;
        }

        void IRenderingBackend.Finalize()
        {
            initialized = false;
        }

        (Allocation renderer, Allocation instance) IRenderingBackend.Create(in Destination destination, in USpan<FixedString> extensionNames)
        {
            Allocation renderer = Allocation.CreateFromValue(new TestRenderer(destination, extensionNames));
            renderingMachines.Add(renderer);
            return (renderer, renderer);
        }

        void IRenderingBackend.Dispose(in Allocation renderer)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.Dispose();
            renderingMachines.Remove(renderer);
            renderer.Dispose();
        }

        void IRenderingBackend.SurfaceCreated(in Allocation renderer, Allocation surface)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.surface = surface;
        }

        StatusCode IRenderingBackend.BeginRender(in Allocation renderer, in Vector4 clearColor)
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

        void IRenderingBackend.Render(in Allocation renderer, in USpan<uint> entities, in MaterialData material, in MeshData mesh, in VertexShaderData vertexShader, in FragmentShaderData fragmentShader)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.entities.AddRange(entities);
            testRenderer.material = material;
            testRenderer.mesh = mesh;
            testRenderer.vertexShader = vertexShader;
            testRenderer.fragmentShader = fragmentShader;
        }

        void IRenderingBackend.EndRender(in Allocation renderer)
        {
            ref TestRenderer testRenderer = ref renderer.Read<TestRenderer>();
            testRenderer.finishedRendering = true;
        }
    }

    public struct TestRenderer : IDisposable
    {
        public readonly Destination destination;
        public readonly Array<FixedString> extensionNames;
        public readonly List<uint> entities;
        public Vector4 clearColor;
        public MaterialData material;
        public MeshData mesh;
        public VertexShaderData vertexShader;
        public FragmentShaderData fragmentShader;
        public Allocation surface;
        public bool finishedRendering;

        public TestRenderer(Destination destination, USpan<FixedString> extensionNames)
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
