using System;
using System.Collections.Generic;

namespace Rendering.Systems.Tests
{
    public class TestRendererBackend : RenderingBackend
    {
        public static bool initialized;
        public readonly List<TestRenderer> renderingMachines = new();

        public override ReadOnlySpan<char> Label => "test";

        public TestRendererBackend()
        {
            initialized = true;
        }

        public override void Dispose()
        {
            initialized = false;
        }

        public override RenderingMachine CreateRenderingMachine(Destination destination)
        {
            TestRenderer renderingMachine = new(destination, destination.Extensions);
            renderingMachines.Add(renderingMachine);
            return renderingMachine;
        }
    }
}
