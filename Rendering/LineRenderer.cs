using GoonED.Shaders;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace GoonED.Rendering
{
    public class LineRenderer
    {
        private static int MAX_LINES = 2048;

        private static float[] vertexArray = new float[MAX_LINES * 7 * 2];

        private static int vaoID;
        private static int vboID;

        private static bool started = false;

        class Line
        {
            public float startX, startY;
            public float endX, endY;
            public float r, g, b, a;

            private int lifetime = 0;

            public Line(float startX, float startY, float endX, float endY, float r, float g, float b, float a, int lifetime)
            {
                this.startX = startX; this.startY = startY;
                this.endX = endX; this.endY = endY;
                this.r = r; this.g = g; this.b = b; this.a = a;
                this.lifetime = lifetime;
            }

            public Line(Vector2 start, Vector2 end, Vector4 color, int lifetime)
            {
                startX = start.X; startY = start.Y;
                endX = end.X; endY = end.Y;
                r = color.X; g = color.Y; b = color.Z; a= color.W;
                this.lifetime = lifetime;
            }

            public int BeginFrame()
            {
                lifetime--;
                return lifetime;
            }
        }

        private static List<Line> lines = new List<Line>();

        private static Shader lineShader;

        public static void Startup()
        {
            lineShader = new Shader(Shader.ParseShader("debug.shader"), true);

            vaoID = GL.GenVertexArray();
            GL.BindVertexArray(vaoID);

            vboID = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, new nint(vertexArray.Length * sizeof(float)), vertexArray, BufferUsageHint.DynamicDraw);

            // Position attribute
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            //GL.LineWidth(1.0f);

            started = true;
        }

        public static void BeginFrame()
        {
            if (!started)
                Startup();

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].BeginFrame() < 0)
                {
                    lines.RemoveAt(i);
                    i--;
                }
            }
        }

        public static void AddLine(Vector2 from, Vector2 to, Vector4 color, int lifetime)
        {
            if (lines.Count >= MAX_LINES) { Console.WriteLine("Too many lines!"); return; }
            lines.Add(new Line(new Vector2(from.X, from.Y), new Vector2(to.X, to.Y), color, lifetime));
        }

        public static void AddLine(float startX, float startY, float endX, float endY, float r, float g, float b, float a, int lifetime)
        {
            if (lines.Count >= MAX_LINES) { Console.WriteLine("Too many lines!"); return; }
            lines.Add(new Line(startX, startY, endX, endY, r, g, b, a, lifetime));
        }

        public static void Draw(Camera camera)
        {
            if (lines.Count <= 0) return;

            int index = 0;
            foreach (Line line in lines)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = i == 0 ? new Vector2(line.startX, line.startY) : new Vector2(line.endX, line.endY);

                    vertexArray[index + 0] = position.X;
                    vertexArray[index + 1] = position.Y;
                    vertexArray[index + 2] = -10.0f;

                    vertexArray[index + 3] = line.r;
                    vertexArray[index + 4] = line.g;
                    vertexArray[index + 5] = line.b;
                    vertexArray[index + 6] = line.a;
                    index += 7;
                }
            }

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferSubData(BufferTarget.ArrayBuffer, 0, new nint(index * sizeof(float)), vertexArray);

            lineShader.Bind();
            lineShader.SetMatrix("projectionMatrix", camera.ProjectionMatrix);
            lineShader.SetMatrix("viewMatrix", camera.GetViewMatrix());

            GL.BindVertexArray(vaoID);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            GL.DrawArrays(PrimitiveType.Lines, 0, lines.Count);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.Blend);

            lineShader.Unbind();
        }

        public static void Cleanup()
        {
            GL.DeleteBuffer(vboID);
            GL.DeleteVertexArray(vaoID);

            lineShader.Cleanup();
        }
    }
}
