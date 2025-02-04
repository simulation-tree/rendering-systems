using Materials;
using Worlds;

namespace Rendering
{
    public readonly struct MaterialData
    {
        public readonly uint entity;
        public readonly uint version;

        public MaterialData(uint entity, uint version)
        {
            this.entity = entity;
            this.version = version;
        }

        public readonly Material Get(World world)
        {
            return new Entity(world, entity).As<Material>();
        }
    }
}