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
            Assert.That(TestRendererBackend.initialized, Is.False);

            RenderingSystems renderingSystems = simulator.AddSystem(new RenderingSystems());
            renderingSystems.RegisterRenderingBackend<TestRendererBackend>();

            Assert.That(TestRendererBackend.initialized, Is.True);

            simulator.RemoveSystem<RenderingSystems>();

            Assert.That(TestRendererBackend.initialized, Is.False);
        }

        [Test]
        public void CreatesRendererForDestination()
        {
            Destination testDestination = new(world, new(200, 200), RenderingBackend.GetLabel<TestRendererBackend>());
            RenderingSystems renderingSystems = simulator.AddSystem(new RenderingSystems());
            renderingSystems.RegisterRenderingBackend<TestRendererBackend>();

            simulator.Update();

            Assert.That(TestRendererBackend.renderingMachines.Count, Is.EqualTo(1));
            TestRenderer testRenderer = TestRendererBackend.renderingMachines[0].Read<TestRenderer>();
            Assert.That(testRenderer.destination, Is.EqualTo(testDestination));
        }

        [Test]
        public void IterateThroughRendererObjects()
        {
            Destination destination = new(world, new(200, 200), RenderingBackend.GetLabel<TestRendererBackend>());
            destination.AddComponent(new SurfaceInUse());

            RenderingSystems renderingSystems = simulator.AddSystem(new RenderingSystems());
            renderingSystems.RegisterRenderingBackend<TestRendererBackend>();

            Mesh mesh = new(world);
            Shader vertexShader = new(world, ShaderType.Vertex);
            Shader fragmentShader = new(world, ShaderType.Fragment);
            Material material = new(world, vertexShader, fragmentShader);
            MeshRenderer meshRenderer = new(world, mesh, material);
            Viewport viewport = new(world, destination);

            simulator.Update();

            Assert.That(TestRendererBackend.renderingMachines.Count, Is.EqualTo(1));
            TestRenderer testRenderer = TestRendererBackend.renderingMachines[0].Read<TestRenderer>();
            Assert.That(testRenderer.destination, Is.EqualTo(destination));
            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));

            meshRenderer.IsEnabled = false;
            simulator.Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateAndDestroyRendererObjects()
        {
            Destination destination = new(world, new(200, 200), RenderingBackend.GetLabel<TestRendererBackend>());
            destination.AddComponent(new SurfaceInUse());

            RenderingSystems renderingSystems = simulator.AddSystem(new RenderingSystems());
            renderingSystems.RegisterRenderingBackend<TestRendererBackend>();

            Mesh mesh = new(world);
            Shader vertexShader = new(world, ShaderType.Vertex);
            Shader fragmentShader = new(world, ShaderType.Fragment);
            Material material = new(world, vertexShader, fragmentShader);
            MeshRenderer meshRenderer = new(world, mesh, material);
            Viewport viewport = new(world, destination);

            simulator.Update();

            Assert.That(TestRendererBackend.renderingMachines.Count, Is.EqualTo(1));

            TestRenderer testRenderer = TestRendererBackend.renderingMachines[0].Read<TestRenderer>();
            Assert.That(testRenderer.destination, Is.EqualTo(destination));
            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));
            Assert.That(testRenderer.entities[0], Is.EqualTo(meshRenderer.value));

            meshRenderer.Dispose();

            simulator.Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(0));

            meshRenderer = new(world, mesh, material);

            simulator.Update();

            Assert.That(testRenderer.entities.Count, Is.EqualTo(1));
            Assert.That(testRenderer.entities[0], Is.EqualTo(meshRenderer.value));
        }
    }
}
