using System;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.OpenGLBinding;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using VeldridTests.ImGui;

namespace Main
{
    class MainFile
    {
        private static CommandList _CL;
        private static DeviceBuffer _VB;
        private static DeviceBuffer _IB;
        private static Pipeline _Pipeline;
        private static Shader[] _Shaders;
        private static GraphicsDevice _GD;

        private static string? VertexShader;
        private static string? FragmentShader;

        public int Delta = 0;

        static void Main(string[] args)
        {
            GraphicsDeviceOptions Options = new GraphicsDeviceOptions
            {
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
            };

            WindowCreateInfo WindowCL = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 800,
                WindowHeight = 600,
                WindowTitle = "Veldrid Test Window",
            };

            Sdl2Window Window = VeldridStartup.CreateWindow(ref WindowCL);
            _GD = VeldridStartup.CreateGraphicsDevice(Window, Options);

            CreateResources();
            Gui.Initialize(_GD, _CL, Window);

            while (Window.Exists)
            {
                Window.PumpEvents();
                Draw();
            }

            DisposeResources();
        }

        private static void Draw()
        {
            _CL.Begin();
            _CL.SetFramebuffer(_GD.SwapchainFramebuffer);
            _CL.ClearColorTarget(0, RgbaFloat.Black);
            _CL.SetVertexBuffer(0, _VB);
            _CL.SetIndexBuffer(_IB, IndexFormat.UInt16);
            _CL.SetPipeline(_Pipeline);
            _CL.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0
            );
            Gui.NewFrame();
            ImGuiNET.ImGui.ShowDemoWindow();
            ImGuiNET.ImGui.EndFrame();
            Gui.Render(_GD, _CL);

            _CL.End();
            _GD.SubmitCommands(_CL);
            _GD.SwapBuffers();
        }

        private static void CreateResources()
        {
            ResourceFactory factory = _GD.ResourceFactory;
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();

            Console.WriteLine("Checking shaders...");
            bool isGlslVert = ShaderSimpleReader.IsGlsl("Shaders/Vertex.vert");
            bool isGlslFrag = ShaderSimpleReader.IsGlsl("Shaders/Fragment.frag");
            if (!isGlslVert || !isGlslFrag)
            {
                throw new InvalidOperationException(
                    "ShaderSimpleReader only supports GLSL .vert and .frag files."
                );
            }
            else
            {
                Console.WriteLine("Shaders are valid GLSL files... \nCompiling shaders...");
            }

            VertexShader = ShaderSimpleReader.ReadText("Shaders/Vertex.vert");
            FragmentShader = ShaderSimpleReader.ReadText("Shaders/Fragment.frag");

            ShaderDescription vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VertexShader),
                "main"
            );
            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FragmentShader),
                "main"
            );
            _Shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
            Console.WriteLine("Shaders compiled...");

            VertexPositionColor[] QuadVertices =
            {
                new VertexPositionColor(new Vector2(-0.75f, 0.75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(0.75f, 0.75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-0.75f, -0.75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(0.75f, -0.75f), RgbaFloat.Yellow),
            };

            ushort[] QuadIndices = { 0, 1, 2, 3 };

            _VB = factory.CreateBuffer(
                new BufferDescription(4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer)
            );
            _IB = factory.CreateBuffer(
                new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer)
            );

            _GD.UpdateBuffer(_VB, 0, QuadVertices);
            _GD.UpdateBuffer(_IB, 0, QuadIndices);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription(
                    "Position",
                    VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2
                ),
                new VertexElementDescription(
                    "Color",
                    VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float4
                )
            );

            Console.WriteLine("Creating pipeline(Graphics)...");
            CreateGraphicsPipeline(_GD, factory, pipelineDescription, vertexLayout);
            Console.WriteLine("Pipeline created(Graphics)...");

            _CL = factory.CreateCommandList();
        }

        private static void DisposeResources()
        {
            _Pipeline.Dispose();
            _VB.Dispose();
            _IB.Dispose();
            _CL.Dispose();
            _GD.Dispose();
        }

        /// <summary
        /// Creates a graphics pipeline;
        /// /summary>
        public static void CreateGraphicsPipeline(
            GraphicsDevice GD,
            ResourceFactory factory,
            GraphicsPipelineDescription pipelineDescription,
            VertexLayoutDescription vertexLayout
        )
        {
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual
            );
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            pipelineDescription.ResourceLayouts = Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _Shaders
            );
            pipelineDescription.Outputs = GD.SwapchainFramebuffer.OutputDescription;
            _Pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        }

        struct VertexPositionColor
        {
            public Vector2 Position; // This is the position, in normalized device coordinates.
            public RgbaFloat Color; // This is the color of the vertex.

            public VertexPositionColor(Vector2 position, RgbaFloat color)
            {
                Position = position;
                Color = color;
            }

            public const uint SizeInBytes = 24;
        }
    }

    /// <summary
    /// Shader reader class;
    /// /summary>
    public static class ShaderSimpleReader
    {
        public static bool IsGlsl(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".vert" || ext == ".frag" || ext == ".vs" || ext == ".fs";
        }

        public static string ReadText(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("Shader file not found", path);
            if (!IsGlsl(path))
                throw new InvalidOperationException(
                    "Only .vert/.frag GLSL files are supported by ShaderSimpleReader."
                );
            return File.ReadAllText(path, Encoding.UTF8);
        }

        public static byte[] ReadBytes(string path)
        {
            var txt = ReadText(path);
            return Encoding.UTF8.GetBytes(txt);
        }
    }
}
