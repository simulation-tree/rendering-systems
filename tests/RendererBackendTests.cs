using Materials;
using Meshes;
using Rendering.Components;
using Shaders;

namespace Rendering.Systems.Tests
{
    public class RendererBackendTests : RenderingSystemTests
    {
        [Test]
        public void CheckInitialization()
        {
            simulator.Add(new RenderingSystems(simulator));

            Assert.That(TestRendererBackend.initialized, Is.False);

            RegisterRenderingBackend(new TestRendererBackend());

            Assert.That(TestRendererBackend.initialized, Is.True);

            simulator.Remove<RenderingSystems>();

            Assert.That(TestRendererBackend.initialized, Is.False);
        }

        [Test]
        public void CreatesRendererForDestination()
        {
            Destination testDestination = new(world, new(200, 200), "test");

            simulator.Add(new RenderingSystems(simulator));
            TestRendererBackend testRendererBackend = new();
            RegisterRenderingBackend(testRendererBackend);

            Update();

            Assert.That(testRendererBackend.renderingMachines.Count, Is.EqualTo(1));
            TestRenderer testRenderer = testRendererBackend.renderingMachines[0];
            Assert.That(testRenderer.destination, Is.EqualTo(testDestination));

            simulator.Remove<RenderingSystems>();
        }

        [Test]
        public void IterateThroughRendererObjects()
        {
            Destination destination = new(world, new(200, 200), "test");
            destination.AddComponent(new SurfaceInUse());

            simulator.Add(new RenderingSystems(simulator));
            TestRendererBackend testRendererBackend = new();
            RegisterRenderingBackend(testRendererBackend);

            Mesh mesh = new(world);
            Shader vertexShader = new(world, ShaderType.Vertex);
            Shader fragmentShader = new(world, ShaderType.Fragment);
            Material material = new(world, vertexShader, fragmentShader);
            MeshRenderer meshRenderer = new(world, mesh, material);
            Viewport viewport = new(world, destination);

            Update();

            Assert.That(testRendererBackend.renderingMachines.Count, Is.EqualTo(1));
            TestRenderer testRenderer = testRendererBackend.renderingMachines[0];
            Assert.That(testRenderer.destination, Is.EqualTo(destination));
            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));

            meshRenderer.IsEnabled = false;
            Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(0));

            simulator.Remove<RenderingSystems>();
        }

        [Test]
        public void CreateAndDestroyRendererObjects()
        {
            Destination destination = new(world, new(200, 200), "test");
            destination.AddComponent(new SurfaceInUse());

            simulator.Add(new RenderingSystems(simulator));
            TestRendererBackend testRendererBackend = new();
            RegisterRenderingBackend(testRendererBackend);

            Mesh mesh = new(world);
            Shader vertexShader = new(world, ShaderType.Vertex);
            Shader fragmentShader = new(world, ShaderType.Fragment);
            Material material = new(world, vertexShader, fragmentShader);
            MeshRenderer meshRenderer = new(world, mesh, material);
            Viewport viewport = new(world, destination);

            Update();

            Assert.That(testRendererBackend.renderingMachines.Count, Is.EqualTo(1));

            TestRenderer testRenderer = testRendererBackend.renderingMachines[0];
            Assert.That(testRenderer.destination, Is.EqualTo(destination));
            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));

            RenderEntity firstEntity = new(meshRenderer.value, mesh.value, material.value, vertexShader.value, fragmentShader.value, mesh.Version, vertexShader.Version, fragmentShader.Version);
            Assert.That(testRenderer.entities[0], Is.EqualTo(firstEntity));

            meshRenderer.Dispose();

            Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(0));

            meshRenderer = new(world, mesh, material);

            Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));
            Assert.That(testRenderer.entities[0], Is.EqualTo(firstEntity));

            simulator.Remove<RenderingSystems>();
        }
    }
}
