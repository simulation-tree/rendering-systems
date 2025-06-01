using System;
using System.Collections.Generic;
using Unmanaged;

namespace Rendering.Systems.Tests
{
    public class TestRendererBackend : RenderingBackend<TestRenderer>
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

        public override TestRenderer Create(Destination destination)
        {
            TestRenderer renderingMachine = new(destination, MemoryAddress.AllocateValue(1337), destination.Extensions);
            renderingMachines.Add(renderingMachine);
            return renderingMachine;
        }

        public override void Dispose(TestRenderer renderingMachine)
        {
            renderingMachine.instance.Dispose();
            renderingMachine.Dispose();
        }
    }
}
