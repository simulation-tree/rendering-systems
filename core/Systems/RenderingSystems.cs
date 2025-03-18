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

        readonly void IDisposable.Dispose()
        {
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
            if (context.World == world)
            {
                context.AddSystem(new ClampNestedScissorViews());
                SystemContainer<RenderEngineSystem> renderEngine = context.AddSystem(new RenderEngineSystem());
                context.Write(new RenderingSystems(renderEngine));
            }
        }

        readonly void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
        }

        readonly void ISystem.Finish(in SystemContext context, in World world)
        {
            if (context.World == world)
            {
                context.RemoveSystem<RenderEngineSystem>();
                context.RemoveSystem<ClampNestedScissorViews>();
            }
        }

        public readonly void RegisterRenderingBackend<T>() where T : unmanaged, IRenderingBackend
        {
            RenderEngineSystem system = renderEngine;
            system.RegisterRenderingBackend<T>();
        }
    }
}