using System;
using Veldrid;
using Veldrid.StartupUtilities;
using Veldrid.Sdl2;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;

namespace Main
{
    class MainFile
    {
        private static CommandList CL;
        private static DeviceBuffer _VB;
        private static DeviceBuffer _IB;
        private static Pipeline _Pipeline;
        private static Shader[] _Shaders;
        private static  GraphicsDevice _GD;

        private static string? VertexShader;
        private static string? FragmentShader;
        

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
                WindowTitle = "Veldrid Test Window"
            };

            Sdl2Window Window = VeldridStartup.CreateWindow(ref WindowCL);
            _GD = VeldridStartup.CreateGraphicsDevice(Window, Options);

            CreateResources();

            while (Window.Exists)
            {
                Window.PumpEvents();
            }
        }


        

        private static void CreateResources()
        {            
            Console.WriteLine("Checking shaders...");
            bool isGlslVert = ShaderSimpleReader.IsGlsl("Shaders/Vertex.vert");
            bool isGlslFrag = ShaderSimpleReader.IsGlsl("Shaders/Fragment.frag");
            if (!isGlslVert || !isGlslFrag)
            {
                throw new InvalidOperationException("ShaderSimpleReader only supports GLSL .vert and .frag files.");
            }
            else 
            {
                Console.WriteLine("Shaders are valid GLSL files.");
            }

            VertexShader = ShaderSimpleReader.ReadText("Shaders/Vertex.vert");
            FragmentShader = ShaderSimpleReader.ReadText("Shaders/Fragment.frag");

            ResourceFactory factory = _GD.ResourceFactory;

            VertexPositionColor[] QuadVertices =
            {
                new VertexPositionColor(new Vector2(-0.75f, 0.75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(0.75f, 0.75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-0.75f, -0.75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(0.75f, -0.75f), RgbaFloat.Yellow)
            };

            ushort[] QuadIndices =
            {
                0, 1, 2, 3
            };

            _VB = factory.CreateBuffer(new BufferDescription(4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
            _IB = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));

            _GD.UpdateBuffer(_VB, 0, QuadVertices);
            _GD.UpdateBuffer(_IB, 0, QuadIndices);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));


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
            if (path == null) throw new ArgumentNullException(nameof(path));
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
                throw new InvalidOperationException("Only .vert/.frag GLSL files are supported by ShaderSimpleReader.");
            return File.ReadAllText(path, Encoding.UTF8);
        }

        public static byte[] ReadBytes(string path)
        {
            var txt = ReadText(path);
            return Encoding.UTF8.GetBytes(txt);
        }
    }
}