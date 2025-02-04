using Meshes;
using Worlds;

namespace Rendering
{
    public readonly struct MeshData
    {
        public readonly uint entity;
        public readonly uint version;
        public MeshData(uint entity, uint version)
        {
            this.entity = entity;
            this.version = version;
        }

        public readonly Mesh Get(World world)
        {
            return new Entity(world, entity).As<Mesh>();
        }
    }
}