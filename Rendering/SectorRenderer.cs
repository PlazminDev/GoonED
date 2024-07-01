using GoonED.Shaders;
using OpenTK.Graphics.OpenGL;

using Sector = GoonED.GoonED.Sector;

namespace GoonED.Rendering
{
    public class SectorRenderer
    {
        private Shader shader;

        public SectorRenderer()
        {
            shader = new Shader(Shader.ParseShader("sectors.shader"), true);
        }

        public void Render(List<Sector> sectors, Camera _camera)
        {
            shader.Bind();

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            float alpha;
            foreach (Sector sector in sectors)
            {
                shader.SetMatrix("projectionMatrix", _camera.ProjectionMatrix);
                shader.SetMatrix("viewMatrix", _camera.GetViewMatrix());

                alpha = 0.2f;
                if (sector.hovered) alpha += 0.2f;
                if (sector.selected) alpha = 0.6f;

                shader.SetFloat("alpha", alpha);

                GL.BindVertexArray(sector.GetVaoID());
                GL.DrawElements(BeginMode.Triangles, sector.indices.Length, DrawElementsType.UnsignedInt, 0);
            }

            GL.BindVertexArray(0);

            GL.Disable(EnableCap.Blend);

            shader.Unbind();
        }

        public void Cleanup()
        {
            shader.Cleanup();
        }
    }
}
