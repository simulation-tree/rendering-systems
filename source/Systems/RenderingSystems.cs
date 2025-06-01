using Simulation;
using Worlds;

namespace Rendering.Systems
{
    public class RenderingSystems : SystemBase
    {
        private readonly RenderEngineSystem renderEngine;

        public RenderingSystems(Simulator simulator, World world) : base(simulator)
        {
            renderEngine = new(simulator, world);
            simulator.Add(renderEngine);
            simulator.Add(new ClampNestedScissorViews(simulator, world));
        }

        public override void Dispose()
        {
            simulator.Remove<ClampNestedScissorViews>();
            simulator.Remove(renderEngine);
            renderEngine.Dispose();
        }

        public void RegisterRenderingBackend<T>(T renderingBackend) where T : RenderingBackend
        {
            renderEngine.RegisterRenderingBackend(renderingBackend);
        }

        public void UnregisterRenderingBackend<T>() where T : RenderingBackend
        {
            renderEngine.UnregisterRenderingBackend<T>();
        }
    }
}