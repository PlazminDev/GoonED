using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Vector3f = System.Numerics.Vector3;
using Vector2f = System.Numerics.Vector2;
using System.Text;
using Json.Net;

namespace GoonED
{
    public class GoonED : GameWindow
    {
        private ImGuiController _ImGuiController;

        // Map parameters
        private byte[] mapName = new byte[32];
        private Vector3f fogColor = new Vector3f(0, 0, 0);

        bool leftMouse = false;
        bool rightMouse = false;
        bool middleMouse = false;

        string AcesDataPath = null;
        private byte[] TempAcesPath = new byte[256];

        // Other
        int startEditIndex_Vertex = -1;
        int startEditIndex_Line = -1;

        private Camera _camera;

        struct Sector
        {
            public int[] vertices { get; set; }
            public int[] lines { get; set; }
            public float floor { get; set; }
            public float ceiling { get; set; }
            public int layer { get; set; }

            //private bool selected = false;

            public Sector(int[] vertices, int[] lines, float floor, float ceiling, int layer)
            {
                this.vertices = vertices;
                this.lines = lines;
                this.floor = floor;
                this.ceiling = ceiling;
                this.layer = layer;
            }
        }

        struct Vertex
        {
            public float x { get; set; } = 0.0f;
            public float y { get; set; } = 0.0f;

            public Vertex(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }

        struct Line
        {
            public int a { get; set; }
            public int b { get; set; }

            public int sector { get; set; } = -1;

            public Line(int a, int b)
            {
                this.a = a;
                this.b = b;
            }

            public void SetSector(int sector)
            {
                this.sector = sector;
            }
        }

        private class Map
        {
            public List<Vector2> Vertices = new();
            public List<Line> Lines = new();
            public List<Sector> Sectors = new();
        }

        private class SerializedMap
        {
            public Vertex[] Vertices { get; set; }
            public Line[] Lines { get; set; }
            public Sector[] Sectors { get; set; }

            public SerializedMap(List<Vector2> Vertices, List<Line> Lines, List<Sector> Sectors)
            {
                this.Vertices = new Vertex[Vertices.Count];
                for(int i = 0; i < Vertices.Count; i++)
                {
                    this.Vertices[i] = new Vertex(Vertices[i].X, Vertices[i].Y);
                }
                this.Lines = Lines.ToArray();
                this.Sectors = Sectors.ToArray();
            }
        }

        Map currentMap;

        public GoonED() : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = new Vector2i(1600, 900), APIVersion = new Version(3, 3) })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            Title = "GoonED";
            VSync = VSyncMode.On;

            _ImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);

            currentMap = new Map();
            currentMap.Vertices.Add(new Vector2(5.0f, 1.0f));
            currentMap.Vertices.Add(new Vector2(-5.0f, -1.0f));
            currentMap.Lines.Add(new Line(0, 1));
            currentMap.Sectors.Add(new Sector(new int[] { 0, 1 }, new int[] { 0 }, 10.0f, 5.0f, 0));

            _camera = new Camera(ClientSize.X, ClientSize.Y);

            // Ini file
            if (File.Exists("user.ini"))
            {
                AcesDataPath = File.ReadAllText("user.ini");
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            _ImGuiController.WindowResized(ClientSize.X, ClientSize.Y);
            _camera.RefreshMatrix(ClientSize.X, ClientSize.Y); 
        }

        protected override void OnUnload()
        {
            PointRenderer.Cleanup();
            LineRenderer.Cleanup();
            _ImGuiController.Dispose();
            base.OnUnload();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            //_camera.position.Y += (float)args.Time;

            //Console.WriteLine(leftMouse + ", " + rightMouse);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            _ImGuiController.Update(this, (float)(args.Time), out bool inputConsumed);
            PointRenderer.BeginFrame();
            LineRenderer.BeginFrame();

            GL.ClearColor(new Color4(0, 0, 0, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            for (int i = (int)-_camera.orthographicSize; i < (int)_camera.orthographicSize; i++)
            {
                LineRenderer.AddLine(new Vector2(i, -_camera.orthographicSize), new Vector2(i, _camera.orthographicSize),
                    i == 0 ? new Vector3(0.3f, 1.0f, 0.3f) : new Vector3(0.5f, 0.5f, 0.5f), 1);
                LineRenderer.AddLine(new Vector2(-_camera.orthographicSize, i), new Vector2(_camera.orthographicSize, i),
                    i == 0 ? new Vector3(1.0f, 0.3f, 0.3f) : new Vector3(0.5f, 0.5f, 0.5f), 1);
            }

            Vector2 mousePos = _camera.GetRelativeMousePos(MousePosition);
            if (!inputConsumed)
            {
                LineRenderer.AddLine(new Vector2(0, 0), mousePos,
                    new Vector3(1.0f, 1.0f, 0f), 1);

                PointRenderer.AddPoint(mousePos, new Vector3(1.0f, 1.0f, 0.0f), 1);
            }

            LineRenderer.Draw(_camera);
            PointRenderer.Draw(_camera);

            //ImGui.DockSpaceOverViewport();

            if (AcesDataPath != null)
            {
                ImGui.Begin("Editor");
                Vector2f buttonSize = new Vector2f(75.0f, 0.0f);
                ImGui.InputText("Map Name", mapName, 50);
                ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                if (ImGui.Button("Save", buttonSize))
                {
                    if (!Directory.Exists("saves"))
                        Directory.CreateDirectory("saves");

                    string _mapName = Encoding.Default.GetString(mapName).Replace("\0", string.Empty).Trim();

                    SerializedMap serializedMap = new SerializedMap(currentMap.Vertices, currentMap.Lines, currentMap.Sectors);
                    string _map = JsonNet.Serialize(serializedMap);

                    string data =
                        "// DO NOT REMOVE THIS\n// Do not use this as the actual map file, this is just a save file for GoonED, " +
                        "if you wanna get this as a Fallen Aces map, use the Export button.\n"
                        + _map;
                    Console.WriteLine(data);

                    FileStream file = File.Create("saves/" + _mapName + ".goon");
                    file.Write(Encoding.ASCII.GetBytes(data));
                    file.Close();
                }
                if (ImGui.Button("Load", buttonSize))
                {

                }
                if (ImGui.Button("Export", buttonSize))
                {

                }
            }

            if(AcesDataPath == null)
            {
                ImGuiWindowFlags window_flags =
                    ImGuiWindowFlags.NoDecoration
                    | ImGuiWindowFlags.AlwaysAutoResize
                    | ImGuiWindowFlags.NoSavedSettings
                    | ImGuiWindowFlags.NoFocusOnAppearing
                    | ImGuiWindowFlags.NoNav
                    | ImGuiWindowFlags.NoResize
                    | ImGuiWindowFlags.NoDocking
                    | ImGuiWindowFlags.NoBringToFrontOnFocus;
                ImGui.SetNextWindowPos(new Vector2f(-5, -5));
                ImGui.SetNextWindowSize(new Vector2f(ClientSize.X+5, ClientSize.Y+5));
                ImGui.Begin("NoInteractPanel", window_flags);
                ImGui.End();

                ImGui.Begin("Setup");
                ImGui.Text("Enter the path to your AcesData folder.");
                ImGui.InputText("###", TempAcesPath, 256);
                if(ImGui.Button("Confirm"))
                {
                    AcesDataPath = Encoding.Default.GetString(TempAcesPath).Replace("\0", string.Empty).Trim();

                    FileStream file = File.Create("user.ini");
                    file.Write(TempAcesPath);
                    file.Close();
                }
                ImGui.End();
            }

            //ImGui.ColorEdit3("Fog Color", ref fogColor);
            ImGui.End();

            _ImGuiController.Render();

            ImGuiController.CheckGLError("End of frame");

            SwapBuffers();
            base.OnRenderFrame(args);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);


            _ImGuiController.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _ImGuiController.MouseScroll(e.Offset);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if(middleMouse)
            {
                _camera.position.X -= e.DeltaX * 0.01f;
                _camera.position.Y += e.DeltaY * 0.01f;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if(e.Button == MouseButton.Left)
                leftMouse = true;
            if(e.Button == MouseButton.Right)
                rightMouse = true;
            if (e.Button == MouseButton.Middle)
                middleMouse = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButton.Left)
                leftMouse = false;
            if (e.Button == MouseButton.Right)
                rightMouse = false;
            if (e.Button == MouseButton.Middle)
                middleMouse = false;
        }
    }
}
