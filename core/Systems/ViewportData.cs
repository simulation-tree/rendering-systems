namespace Rendering.Systems
{
    public readonly struct ViewportData
    {
        public readonly Viewport entity;
        public readonly LayerMask renderMask;
        public readonly Destination destination;

        public ViewportData(Viewport entity, LayerMask renderMask, Destination destination)
        {
            this.entity = entity;
            this.renderMask = renderMask;
            this.destination = destination;
        }
    }
}