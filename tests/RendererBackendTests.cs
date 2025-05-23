using Materials;
using Meshes;
using Rendering.Components;
using Shaders;
using Simulation;

namespace Rendering.Systems.Tests
{
    public class RendererBackendTests : RenderingSystemTests
    {
        [Test]
        public void CheckInitialization()
        {
            Simulator.Add(new RenderingSystems(Simulator));

            Assert.That(TestRendererBackend.initialized, Is.False);

            RegisterRenderingBackend(new TestRendererBackend());

            Assert.That(TestRendererBackend.initialized, Is.True);

            Simulator.Remove<RenderingSystems>();

            Assert.That(TestRendererBackend.initialized, Is.False);
        }

        [Test]
        public void CreatesRendererForDestination()
        {
            Destination testDestination = new(world, new(200, 200), "test");

            Simulator.Add(new RenderingSystems(Simulator));
            TestRendererBackend testRendererBackend = new();
            RegisterRenderingBackend(testRendererBackend);

            Simulator.Update();

            Assert.That(testRendererBackend.renderingMachines.Count, Is.EqualTo(1));
            TestRenderer testRenderer = testRendererBackend.renderingMachines[0];
            Assert.That(testRenderer.destination, Is.EqualTo(testDestination));

            Simulator.Remove<RenderingSystems>();
        }

        [Test]
        public void IterateThroughRendererObjects()
        {
            Destination destination = new(world, new(200, 200), "test");
            destination.AddComponent(new SurfaceInUse());

            Simulator.Add(new RenderingSystems(Simulator));
            TestRendererBackend testRendererBackend = new();
            RegisterRenderingBackend(testRendererBackend);

            Mesh mesh = new(world);
            Shader vertexShader = new(world, ShaderType.Vertex);
            Shader fragmentShader = new(world, ShaderType.Fragment);
            Material material = new(world, vertexShader, fragmentShader);
            MeshRenderer meshRenderer = new(world, mesh, material);
            Viewport viewport = new(world, destination);

            Simulator.Update();

            Assert.That(testRendererBackend.renderingMachines.Count, Is.EqualTo(1));
            TestRenderer testRenderer = testRendererBackend.renderingMachines[0];
            Assert.That(testRenderer.destination, Is.EqualTo(destination));
            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));

            meshRenderer.IsEnabled = false;
            Simulator.Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(0));

            Simulator.Remove<RenderingSystems>();
        }

        [Test]
        public void CreateAndDestroyRendererObjects()
        {
            Destination destination = new(world, new(200, 200), "test");
            destination.AddComponent(new SurfaceInUse());

            Simulator.Add(new RenderingSystems(Simulator));
            TestRendererBackend testRendererBackend = new();
            RegisterRenderingBackend(testRendererBackend);

            Mesh mesh = new(world);
            Shader vertexShader = new(world, ShaderType.Vertex);
            Shader fragmentShader = new(world, ShaderType.Fragment);
            Material material = new(world, vertexShader, fragmentShader);
            MeshRenderer meshRenderer = new(world, mesh, material);
            Viewport viewport = new(world, destination);

            Simulator.Update();

            Assert.That(testRendererBackend.renderingMachines.Count, Is.EqualTo(1));

            TestRenderer testRenderer = testRendererBackend.renderingMachines[0];
            Assert.That(testRenderer.destination, Is.EqualTo(destination));
            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));

            RenderEntity firstEntity = new(meshRenderer.value, mesh.value, material.value, vertexShader.value, fragmentShader.value, mesh.Version, vertexShader.Version, fragmentShader.Version);
            Assert.That(testRenderer.entities[0], Is.EqualTo(firstEntity));

            meshRenderer.Dispose();

            Simulator.Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(0));

            meshRenderer = new(world, mesh, material);

            Simulator.Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));
            Assert.That(testRenderer.entities[0], Is.EqualTo(firstEntity));

            Simulator.Remove<RenderingSystems>();
        }
    }
}
