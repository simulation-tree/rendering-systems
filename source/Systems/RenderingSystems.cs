using Simulation;
using System;
using Worlds;

namespace Rendering.Systems
{
    public readonly partial struct RenderingSystems : ISystem
    {
        private readonly SystemContainer<RenderEngineSystem> renderEngine;

        private RenderingSystems(SystemContainer<RenderEngineSystem> renderEngine)
        {
            this.renderEngine = renderEngine;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                Simulator simulator = systemContainer.simulator;
                simulator.AddSystem<ClampNestedScissorViews>();
                SystemContainer<RenderEngineSystem> renderEngine = simulator.AddSystem<RenderEngineSystem>();

                systemContainer.Write(new RenderingSystems(renderEngine));
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                Simulator simulator = systemContainer.simulator;
                simulator.RemoveSystem<RenderEngineSystem>();
                simulator.RemoveSystem<ClampNestedScissorViews>();
            }
        }

        public readonly void RegisterRenderingBackend<T>() where T : unmanaged, IRenderingBackend
        {
            ref RenderEngineSystem system = ref renderEngine.Value;
            system.RegisterRenderingBackend<T>();
        }
    }
}