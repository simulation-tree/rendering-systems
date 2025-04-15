using Rendering.Components;
using Rendering.Functions;
using Simulation;
using System;
using System.Numerics;
using Unmanaged;

namespace Rendering
{
    /// <summary>
    /// Describes a rendering API.
    /// </summary>
    public interface IRenderingBackend
    {
        /// <summary>
        /// Unique label referenced by <see cref="Destination"/> objects.
        /// </summary>
        ASCIIText256 Label { get; }

        public StartFunction StartFunction => default;
        public FinishFunction FinishFunction => default;
        public Create CreateFunction => default;
        public Dispose DisposeFunction => default;
        public SurfaceCreated SurfaceCreatedFunction => default;
        public BeginRender BeginRenderFunction => default;
        public Render RenderFunction => default;
        public EndRender EndRenderFunction => default;

        /// <summary>
        /// Called when the backend is registered.
        /// </summary>
        void Start();

        /// <summary>
        /// Called when the backend is unregistered.
        /// </summary>
        void Finish();

        /// <summary>
        /// Creates a renderer system for handling render functionality of the given <see cref="Destination"/>.
        /// <para>
        /// The instance is available on <see cref="RendererInstanceInUse"/>
        /// </para>
        /// </summary>
        /// <returns>The renderer that handles render functions, and the instance originating from the API in use.</returns>
        (MemoryAddress machine, MemoryAddress instance) Create(in Destination destination, in ReadOnlySpan<DestinationExtension> extensionNames);

        /// <summary>
        /// Disposes a previously created renderer system.
        /// </summary>
        void Dispose(in MemoryAddress machine, in MemoryAddress instance);

        /// <summary>
        /// Callback when a surface has been created for a renderer.
        /// </summary>
        void SurfaceCreated(in MemoryAddress machine, MemoryAddress surface);

        /// <summary>
        /// Called to prepare for rendering.
        /// </summary>
        StatusCode BeginRender(in MemoryAddress machine, in Vector4 clearColor);

        /// <summary>
        /// Performs rendering of a single frame with the <see cref="Destination"/>
        /// the renderer was created with.
        /// </summary>
        void Render(in MemoryAddress machine, in uint materialEntity, in ushort materialVersion, in ReadOnlySpan<RenderEntity> entities);

        /// <summary>
        /// Finishes rendering a frame.
        /// </summary>
        void EndRender(in MemoryAddress machine);
    }
}