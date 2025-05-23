using Rendering.Components;
using System.Numerics;

namespace Rendering.Systems.Tests
{
    public class ScissorViewTests : RenderingSystemTests
    {
        [Test]
        public void VerifyClampedScissor()
        {
            simulator.Add(new ClampNestedScissorViews());

            uint parentScissor = world.CreateEntity();
            uint childScissor = world.CreateEntity();
            world.SetParent(childScissor, parentScissor);
            world.AddComponent(parentScissor, new RendererScissor(0, 0, 100, 100));
            world.AddComponent(childScissor, new RendererScissor(50, 50, 100, 100));

            Update();

            WorldRendererScissor parent = world.GetComponent<WorldRendererScissor>(parentScissor);
            WorldRendererScissor child = world.GetComponent<WorldRendererScissor>(childScissor);
            Assert.That(parent.value, Is.EqualTo(new Vector4(0, 0, 100, 100)));
            Assert.That(child.value, Is.EqualTo(new Vector4(50, 50, 50, 50)));

            simulator.Remove<ClampNestedScissorViews>();
        }

        [Test]
        public void OutOfBoundsChildScissor()
        {
            simulator.Add(new ClampNestedScissorViews());

            uint parentScissor = world.CreateEntity();
            uint childScissor = world.CreateEntity();
            world.SetParent(childScissor, parentScissor);
            world.AddComponent(parentScissor, new RendererScissor(0, 0, 100, 100));
            world.AddComponent(childScissor, new RendererScissor(0, 200, 100, 20));

            Update();

            WorldRendererScissor parent = world.GetComponent<WorldRendererScissor>(parentScissor);
            WorldRendererScissor child = world.GetComponent<WorldRendererScissor>(childScissor);
            Assert.That(parent.value, Is.EqualTo(new Vector4(0, 0, 100, 100)));
            Assert.That(child.value, Is.EqualTo(new Vector4(0, 100, 100, 0)));

            simulator.Remove<ClampNestedScissorViews>();
        }

        [Test]
        public void DeepNestedChildren()
        {
            simulator.Add(new ClampNestedScissorViews());

            uint rootScissor = world.CreateEntity();
            uint parentScissor = world.CreateEntity();
            uint childScissor = world.CreateEntity();
            world.SetParent(parentScissor, rootScissor);
            world.SetParent(childScissor, parentScissor);
            world.AddComponent(rootScissor, new RendererScissor(0, 0, 100, 100));
            world.AddComponent(parentScissor, new RendererScissor(50, 50, 100, 100));
            world.AddComponent(childScissor, new RendererScissor(25, 25, 50, 50));

            Update();

            WorldRendererScissor root = world.GetComponent<WorldRendererScissor>(rootScissor);
            WorldRendererScissor parent = world.GetComponent<WorldRendererScissor>(parentScissor);
            WorldRendererScissor child = world.GetComponent<WorldRendererScissor>(childScissor);
            Assert.That(root.value, Is.EqualTo(new Vector4(0, 0, 100, 100)));
            Assert.That(parent.value, Is.EqualTo(new Vector4(50, 50, 50, 50)));
            Assert.That(child.value, Is.EqualTo(new Vector4(50, 50, 25, 25)));

            simulator.Remove<ClampNestedScissorViews>();
        }

        [Test]
        public void VerifyDeepDescendant()
        {
            simulator.Add(new ClampNestedScissorViews());

            uint rootEntity = world.CreateEntity();
            uint parentScissor = world.CreateEntity();
            uint childScissor = world.CreateEntity();
            world.SetParent(parentScissor, rootEntity);
            world.SetParent(childScissor, parentScissor);
            world.AddComponent(rootEntity, new RendererScissor(0, 0, 100, 100));
            world.AddComponent(childScissor, new RendererScissor(50, 50, 100, 100));

            Update();

            WorldRendererScissor root = world.GetComponent<WorldRendererScissor>(rootEntity);
            WorldRendererScissor child = world.GetComponent<WorldRendererScissor>(childScissor);
            Assert.That(root.value, Is.EqualTo(new Vector4(0, 0, 100, 100)));
            Assert.That(child.value, Is.EqualTo(new Vector4(50, 50, 50, 50)));

            simulator.Remove<ClampNestedScissorViews>();
        }
    }
}