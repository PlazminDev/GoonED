using GoonED.Shaders;
using OpenTK.Graphics.OpenGL;

namespace GoonED.Rendering
{
    public class SpriteRenderer
    {
        private Shader spriteShader;

        private float[] quadVertices =
        {
            -0.5f, -0.5f, -5.0f,
            -0.5f, 0.5f, -5.0f,
            0.5f, 0.5f, -5.0f,
            0.5f, -0.5f, -5.0f,
        };

        private int[] quadIndices =
        {
            0, 1, 2,
            0, 2, 3
        };

        private float[] UVs =
        {
            0.0f, 0.0f,
            0.0f, 1.0f,
            1.0f, 1.0f,
            1.0f, 0.0f
        };

        private int[] vboIDs;
        private int vaoID;

        public SpriteRenderer()
        {
            spriteShader = new Shader(Shader.ParseShader("sprite.shader"), true);

            // Create quad
            vaoID = GL.GenVertexArray();
            GL.BindVertexArray(vaoID);

            vboIDs = new int[3];

            // Positions VBO
            int vboID = GL.GenBuffer();
            vboIDs[0] = vboID;
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, (nint)(quadVertices.Length * sizeof(float)), quadVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            // UVs VBO
            vboID = GL.GenBuffer();
            vboIDs[1] = vboID;
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, (nint)(UVs.Length * sizeof(float)), UVs, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);

            // Index VBO
            vboID = GL.GenBuffer();
            vboIDs[2] = vboID;
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (nint)(quadIndices.Length * sizeof(int)), quadIndices, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(0);
        }

        public void Render(List<Sprite> sprites, Camera _camera)
        {
            spriteShader.Bind();
            spriteShader.SetMatrix("projectionMatrix", _camera.ProjectionMatrix);
            spriteShader.SetMatrix("viewMatrix", _camera.GetViewMatrix());

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindVertexArray(vaoID);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.ActiveTexture(TextureUnit.Texture0);

            foreach (Sprite sprite in sprites)
            {
                spriteShader.SetMatrix("modelMatrix", sprite.GetModelMatrix());

                spriteShader.SetInt("textureSampler", 0);

                sprite.texture.Bind();

                GL.DrawElements(PrimitiveType.Triangles, quadIndices.Length, DrawElementsType.UnsignedInt, 0);

                sprite.texture.Unbind();
            }

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.Blend);

            spriteShader.Unbind();
        }

        public void Cleanup()
        {
            for (int i = 0; i < vboIDs.Length; i++)
            {
                GL.DeleteBuffer(vboIDs[i]);
            }
            GL.DeleteVertexArray(vaoID);

            spriteShader.Cleanup();
        }
    }
}
