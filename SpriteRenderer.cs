using GoonED.Shaders;
using OpenTK.Graphics.OpenGL;

namespace GoonED
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

        private List<int> vboIDs;
        private int vaoID;

        private void Startup()
        {
            spriteShader = new Shader(Shader.ParseShader("sprite.shader"), true);

            // Create quad
            vaoID = GL.GenVertexArray();
            GL.BindVertexArray(vaoID);

            vboIDs = new();

            // Positions VBO
            int vboID = GL.GenBuffer();
            vboIDs.Add(vboID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(quadVertices.Length * sizeof(float)), quadVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            // UVs VBO
            vboID = GL.GenBuffer();
            vboIDs.Add(vboID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(UVs.Length * sizeof(float)), UVs, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);

            // Index VBO
            vboID = GL.GenBuffer();
            vboIDs.Add(vboID);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(quadIndices.Length * sizeof(int)), quadIndices, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(0);
        }

        public void Cleanup()
        {
            vboIDs.ForEach((id) => GL.DeleteBuffer(id));
            GL.DeleteVertexArray(vaoID);

            spriteShader.Cleanup();
        }
    }
}
