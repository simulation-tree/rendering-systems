using Unmanaged;

namespace Rendering.Functions
{
    /// <summary>
    /// Initializes the rendering backend system.
    /// <para>
    /// Balanced with the <see cref="Finalize"/> function.
    /// </para>
    /// </summary>
    public unsafe readonly struct Initialize
    {
#if NET
        private readonly delegate* unmanaged<Allocation, void> function;

        public Initialize(delegate* unmanaged<Allocation, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Allocation, void> function;

        public Initialize(delegate*<Allocation, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(Allocation renderer)
        {
            function(renderer);
        }
    }
}