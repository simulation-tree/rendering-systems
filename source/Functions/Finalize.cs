using Unmanaged;

namespace Rendering.Functions
{
    /// <summary>
    /// Disposes and cleans up any resources for this render system type.
    /// <para>Called just once and is balanced by the <see cref="Initialize"/> function.</para>
    /// </summary>
    public unsafe readonly struct Finalize
    {
#if NET
        private readonly delegate* unmanaged<Allocation, void> function;

        public Finalize(delegate* unmanaged<Allocation, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Allocation, void> function;

        public Finalize(delegate*<Allocation, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(Allocation allocation)
        {
            function(allocation);
        }
    }
}