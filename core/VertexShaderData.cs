using Shaders;
using Worlds;

namespace Rendering
{
    public readonly struct VertexShaderData
    {
        public readonly uint entity;
        public readonly uint version;

        public VertexShaderData(uint entity, uint version)
        {
            this.entity = entity;
            this.version = version;
        }

        public readonly Shader Get(World world)
        {
            return new Entity(world, entity).As<Shader>();
        }
    }
}