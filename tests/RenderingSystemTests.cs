using Data;
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
            TypeRegistry.Load<DataTypeBank>();
            TypeRegistry.Load<MeshesTypeBank>();
            TypeRegistry.Load<MaterialsTypeBank>();
            TypeRegistry.Load<RenderingTypeBank>();
            TypeRegistry.Load<ShadersTypeBank>();
        }

        protected override Schema CreateSchema()
        {
            Schema schema = base.CreateSchema();
            schema.Load<DataSchemaBank>();
            schema.Load<MeshesSchemaBank>();
            schema.Load<MaterialsSchemaBank>();
            schema.Load<RenderingSchemaBank>();
            schema.Load<ShadersSchemaBank>();
            return schema;
        }
    }
}