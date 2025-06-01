using Materials;
using Meshes;
using Rendering.Messages;
using Shaders;
using Simulation.Tests;
using Types;
using Worlds;

namespace Rendering.Systems.Tests
{
    public abstract class RenderingSystemTests : SimulationTests
    {
        public World world;

        //todo: test for multiple viewports (and having them sorted)
        //todo: test for customizing renderer entities sorted by their material
        static RenderingSystemTests()
        {
            MetadataRegistry.Load<MeshesMetadataBank>();
            MetadataRegistry.Load<MaterialsMetadataBank>();
            MetadataRegistry.Load<RenderingMetadataBank>();
            MetadataRegistry.Load<ShadersMetadataBank>();
        }

        protected void RegisterRenderingBackend<T>(T renderingBackend) where T : RenderingBackend
        {
            RenderingSystems renderingSystems = Simulator.GetFirst<RenderingSystems>();
            renderingSystems.RegisterRenderingBackend(renderingBackend);
        }

        protected void UnregisterRenderingBackend<T>() where T : RenderingBackend
        {
            RenderingSystems renderingSystems = Simulator.GetFirst<RenderingSystems>();
            renderingSystems.UnregisterRenderingBackend<T>();
        }

        protected override void SetUp()
        {
            base.SetUp();
            Schema schema = new();
            schema.Load<MeshesSchemaBank>();
            schema.Load<MaterialsSchemaBank>();
            schema.Load<RenderingSchemaBank>();
            schema.Load<ShadersSchemaBank>();
            world = new(schema);
            Simulator.Add(new RenderingSystems(Simulator, world));
        }

        protected override void TearDown()
        {
            Simulator.Remove<RenderingSystems>();
            world.Dispose();
            base.TearDown();
        }

        protected override void Update(double deltaTime)
        {
            Simulator.Broadcast(new RenderUpdate());
        }
    }
}