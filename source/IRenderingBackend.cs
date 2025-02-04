#pragma warning disable CS0465 //gc isnt even used
using Rendering.Functions;
using Rendering.Components;
using Unmanaged;
using System.Numerics;
using Simulation;

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
        FixedString Label { get; }

        public Initialize InitializeFunction => default;
        public Finalize FinalizeFunction => default;
        public Create CreateFunction => default;
        public Dispose DisposeFunction => default;
        public SurfaceCreated SurfaceCreatedFunction => default;
        public BeginRender BeginRenderFunction => default;
        public Render RenderFunction => default;
        public EndRender EndRenderFunction => default;

        /// <summary>
        /// Called when the backend is registered.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called when the backend is unregistered.
        /// </summary>
        void Finalize();

        /// <summary>
        /// Creates a renderer system for handling render functionality of the given <see cref="Destination"/>.
        /// <para>
        /// The instance is available on <see cref="RendererInstanceInUse"/>
        /// </para>
        /// </summary>
        /// <returns>The renderer that handles render functions, and the instance originating from the API in use.</returns>
        (Allocation renderer, Allocation instance) Create(in Destination destination, in USpan<FixedString> extensionNames);
        
        /// <summary>
        /// Disposes a previously created renderer system.
        /// </summary>
        void Dispose(in Allocation renderer);

        /// <summary>
        /// Callback when a surface has been created for a renderer.
        /// </summary>
        void SurfaceCreated(in Allocation renderer, Allocation surface);

        /// <summary>
        /// Called to prepare for rendering.
        /// </summary>
        StatusCode BeginRender(in Allocation renderer, in Vector4 clearColor);

        /// <summary>
        /// Performs rendering of a single frame with the <see cref="Destination"/>
        /// the renderer was created with.
        /// </summary>
        void Render(in Allocation renderer, in USpan<uint> entities, in MaterialData material, in MeshData mesh, in VertexShaderData vertexShader, in FragmentShaderData fragmentShader);

        /// <summary>
        /// Finishes rendering a frame.
        /// </summary>
        void EndRender(in Allocation renderer);
    }
}