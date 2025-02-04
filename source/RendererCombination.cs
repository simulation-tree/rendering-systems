namespace Rendering
{
    public struct RendererCombination
    {
        public uint material;
        public uint mesh;
        public uint vertexShader;
        public uint fragmentShader;

        public RendererCombination(uint material, uint mesh, uint vertexShader, uint fragmentShader)
        {
            this.material = material;
            this.mesh = mesh;
            this.vertexShader = vertexShader;
            this.fragmentShader = fragmentShader;
        }
    }
}