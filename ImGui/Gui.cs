using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;

namespace VeldridTests.ImGui
{
    public static class Gui
    {
        private static GraphicsDevice _GD;
        private static CommandList _CL;
        private static Sdl2Window _Window;
        private static ImGuiRenderer? _renderer;

        public static void Initialize(GraphicsDevice gd, CommandList cl, Sdl2Window window)
        {
            _GD = gd;
            _CL = cl;
            _Window = window;

            ImGuiNET.ImGui.CreateContext();
            ImGuiNET.ImGui.StyleColorsDark();

            _renderer = new global::Veldrid.ImGuiRenderer(
                gd,
                gd.MainSwapchain.Framebuffer.OutputDescription,
                window.Width,
                window.Height
            );
            _renderer.RecreateFontDeviceTexture(gd);
        }

        public static void NewFrame(float deltaSeconds = 1f / 60f)
        {
            if (_renderer == null)
                return;

            var snapshot = _Window.PumpEvents();
            _renderer.Update(deltaSeconds, snapshot);
            ImGuiNET.ImGui.NewFrame();
        }

        public static void Render(GraphicsDevice gd, CommandList cl)
        {
            if (_renderer == null)
                return;

            ImGuiNET.ImGui.Render();
            _renderer.Render(gd, cl);
        }
    }
}
