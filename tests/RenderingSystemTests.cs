using Materials;
using Meshes;
using Shaders;
using Simulation.Tests;
using Types;
using Worlds;

namespace Rendering.Systems.Tests
{
    public abstract class RenderingSystemTests : SimulationTests
    {
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
            if (Simulator.TryGetFirst(out RenderingSystems? renderingSystems))
            {
                renderingSystems.RegisterRenderingBackend(renderingBackend);
            }
        }

        protected override Schema CreateSchema()
        {
            Schema schema = base.CreateSchema();
            schema.Load<MeshesSchemaBank>();
            schema.Load<MaterialsSchemaBank>();
            schema.Load<RenderingSchemaBank>();
            schema.Load<ShadersSchemaBank>();
            return schema;
        }
    }
}