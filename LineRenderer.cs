using GoonED.Shaders;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace GoonED
{
    public class LineRenderer
    {
        private static int MAX_LINES = 500;

        private static float[] vertexArray = new float[MAX_LINES * 6 * 2];

        private static int vaoID;
        private static int vboID;

        private static bool started = false;

        class Line
        {
            public float startX, startY;
            public float endX, endY;
            public float r, g, b;

            private int lifetime = 0;

            public Line(float startX, float startY, float endX, float endY, float r, float g, float b, int lifetime)
            {
                this.startX = startX; this.startY = startY;
                this.endX = endX; this.endY = endY;
                this.r = r; this.g = g; this.b = b;
                this.lifetime = lifetime;   
            }

            public Line(Vector2 start, Vector2 end, Vector3 color, int lifetime)
            {
                this.startX = start.X; this.startY = start.Y;
                this.endX = end.X; this.endY = end.Y;
                this.r = color.X; this.g = color.Y; this.b = color.Z;
                this.lifetime = lifetime;
            }

            public int BeginFrame()
            {
                this.lifetime--;
                return this.lifetime;
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
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexArray.Length * sizeof(float)), vertexArray, BufferUsageHint.DynamicDraw);

            // Position attribute
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            //GL.LineWidth(1.0f);

            started = true;
        }

        public static void BeginFrame()
        {
            if(!started)
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

        public static void AddLine(Vector2 from, Vector2 to, Vector3 color, int lifetime)
        {
            if (lines.Count >= MAX_LINES) { Console.WriteLine("Too many lines!"); return; }
            lines.Add(new Line(new Vector2(from.X, from.Y), new Vector2(to.X, to.Y), color, lifetime));
        }

        public static void Draw(Camera camera)
        {
            if (lines.Count <= 0) return;

            int index = 0;
            foreach(Line line in lines)
            {
                for(int i = 0; i < 2; i++)
                {
                    Vector2 position = i == 0 ? new Vector2(line.startX, line.startY) : new Vector2(line.endX, line.endY);
                    Vector3 color = new Vector3(line.r, line.g, line.b);

                    vertexArray[index + 0] = position.X;
                    vertexArray[index + 1] = position.Y;
                    vertexArray[index + 2] = -10.0f;

                    vertexArray[index + 3] = color.X;
                    vertexArray[index + 4] = color.Y;
                    vertexArray[index + 5] = color.Z;
                    index += 6;
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)0, new IntPtr(index * sizeof(float)), vertexArray);

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
