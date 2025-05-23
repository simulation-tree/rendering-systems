using Simulation;
using System;

namespace Rendering.Systems
{
    public class RenderingSystems : ISystem, IDisposable
    {
        private readonly Simulator simulator;
        private readonly RenderEngineSystem renderEngine;

        public RenderingSystems(Simulator simulator)
        {
            this.simulator = simulator;

            renderEngine = new();
            simulator.Add(renderEngine);
            simulator.Add(new ClampNestedScissorViews());
        }

        public void Dispose()
        {
            simulator.Remove<ClampNestedScissorViews>();
            simulator.Remove(renderEngine);
            renderEngine.Dispose();
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
        }

        public void RegisterRenderingBackend<T>(T renderingBackend) where T : RenderingBackend
        {
            renderEngine.RegisterRenderingBackend(renderingBackend);
        }

        public T UnregisterRenderingBackend<T>(bool dispose = true) where T : RenderingBackend
        {
            return renderEngine.UnregisterRenderingBackend<T>(dispose);
        }
    }
}