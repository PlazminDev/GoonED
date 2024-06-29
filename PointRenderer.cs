using GoonED.Shaders;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace GoonED
{
    public class PointRenderer
    {
        private static int MAX_POINTS = 2048;

        private static float[] vertexArray = new float[MAX_POINTS * 6];

        private static int vaoID;
        private static int vboID;

        private static bool started = false;

        class Point
        {
            public float X, Y;
            public float r, g, b;

            private int lifetime = 0;

            public Point(float startX, float startY, float r, float g, float b, int lifetime)
            {
                this.X = startX; this.Y = startY;
                this.r = r; this.g = g; this.b = b;
                this.lifetime = lifetime;
            }

            public Point(Vector2 start, Vector3 color, int lifetime)
            {
                this.X = start.X; this.Y = start.Y;
                this.r = color.X; this.g = color.Y; this.b = color.Z;
                this.lifetime = lifetime;
            }

            public int beginFrame()
            {
                this.lifetime--;
                return this.lifetime;
            }

            public override string ToString()
            {
                return "(" + X + ", " + Y + ") " + lifetime;
            }
        }

        private static List<Point> points = new List<Point>();

        private static Shader pointShader;

        public static void Startup()
        {
            pointShader = new Shader(Shader.ParseShader("debug.shader"), true);

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

            GL.PointSize(10.0f);
            //GL.LineWidth(1.0f);

            started = true;
        }

        public static void BeginFrame()
        {
            if (!started)
                Startup();

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].beginFrame() < 0)
                {
                    points.RemoveAt(i);
                    i--;
                }
            }
        }

        public static void AddPoint(Vector2 point, Vector3 color, int lifetime)
        {
            if (points.Count >= MAX_POINTS) { Console.WriteLine("Too many points!"); return; }
            points.Add(new Point(point, color, lifetime));
        }

        public static void Draw(Camera camera)
        {
            if (points.Count <= 0) return;

            int index = 0;
            foreach (Point point in points)
            {
                Vector3 color = new Vector3(point.r, point.g, point.b);

                vertexArray[index + 0] = point.X;
                vertexArray[index + 1] = point.Y;
                vertexArray[index + 2] = -10.0f;

                vertexArray[index + 3] = color.X;
                vertexArray[index + 4] = color.Y;
                vertexArray[index + 5] = color.Z;
                index += 6;
            }

            //Console.WriteLine(points[0]);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, new IntPtr(index * 2), vertexArray);

            pointShader.Bind();
            pointShader.SetMatrix("projectionMatrix", camera.ProjectionMatrix);
            pointShader.SetMatrix("viewMatrix", camera.GetViewMatrix());

            GL.BindVertexArray(vaoID);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            GL.DrawArrays(PrimitiveType.Points, 0, index * 2);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.BindVertexArray(0);

            pointShader.Unbind();
        }

        public static void Cleanup()
        {
            GL.DeleteBuffer(vboID);
            GL.DeleteVertexArray(vaoID);
        }
    }
}
