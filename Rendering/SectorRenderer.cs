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

        public void Render(List<Sector> sectors, Camera _camera, AceTextures textures)
        {
            shader.Bind();

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.ActiveTexture(TextureUnit.Texture0);

            float alpha;
            foreach (Sector sector in sectors)
            {
                shader.SetMatrix("projectionMatrix", _camera.ProjectionMatrix);
                shader.SetMatrix("viewMatrix", _camera.GetViewMatrix());

                shader.SetInt("textureSampler", 0);

                alpha = 0.2f;
                if (sector.hovered) alpha += 0.2f;
                if (sector.selected) alpha = 0.6f;

                shader.SetFloat("alpha", alpha);

                textures.GetTexture(sector.TextureIndex).Bind();

                GL.BindVertexArray(sector.GetVaoID());
                GL.EnableVertexAttribArray(0);
                GL.EnableVertexAttribArray(1);
                GL.DrawElements(BeginMode.Triangles, sector.indices.Length, DrawElementsType.UnsignedInt, 0);

                textures.GetTexture(sector.TextureIndex).Unbind();
            }

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
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
