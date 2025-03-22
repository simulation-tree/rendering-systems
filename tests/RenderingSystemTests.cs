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
        static RenderingSystemTests()
        {
            MetadataRegistry.Load<MeshesTypeBank>();
            MetadataRegistry.Load<MaterialsTypeBank>();
            MetadataRegistry.Load<RenderingTypeBank>();
            MetadataRegistry.Load<ShadersTypeBank>();
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