using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Vector2f = System.Numerics.Vector2;
using Vector3f = System.Numerics.Vector3;
using Vector4f = System.Numerics.Vector4;
using System.Text;
using Json.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GoonED.Rendering;
using StbiSharp;
using static System.Net.Mime.MediaTypeNames;

namespace GoonED
{
    public class GoonED : GameWindow
    {
        private ImGuiController _ImGuiController;

        private Vector3 RED = new Vector3(1.0f, 0.0f, 0.0f);
        private Vector3 YELLOW = new Vector3(1.0f, 1.0f, 0.0f);

        private Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();

        // Map parameters
        private byte[] mapName = new byte[32];
        private Vector3f fogColor = new Vector3f(0, 0, 0);
        private int layer = 0;

        bool leftMouse = false;
        bool rightMouse = false;
        bool middleMouse = false;

        string AcesDataPath = "unused";
        private byte[] TempAcesPath = new byte[256];
        private byte[] LoadMapName = new byte[256];

        SectorRenderer sectorRenderer;
        SpriteRenderer spriteRenderer;

        bool loadPrompt = false;

        // Other
        int startEditIndex_Vertex = -1;
        int startEditIndex_Line = -1;

        private Camera _camera;

        public class Sector
        {
            public int[] vertices { get; set; }
            public int[] lines { get; set; }

            public double floor;

            public double ceiling;

            public int[] indices;

            public int layer { get; set; }

            private int[] vbos;
            private int vaoID;

            public bool hovered = false;
            public bool selected = false;

            public Sector() { }

            public Sector(int[] vertices, int[] lines, double floor, double ceiling, int layer)
            {
                this.vertices = vertices;
                this.lines = lines;
                this.floor = floor;
                this.ceiling = ceiling;
                this.layer = layer;

                vaoID = GL.GenVertexArray();
                GL.BindVertexArray(vaoID);

                this.vbos = new int[2];

                int numVertices = vertices.Length;
                uint[] _indices = new uint[IndexBufferUtil.GetTriangleFanIndexCount(numVertices)];
                IndexBufferUtil.BuildTriangleFan(0, (uint)numVertices, ref _indices);

                this.indices = new int[_indices.Length];
                for(int i = 0; i < _indices.Length; i++) this.indices[i] = (int)_indices[i];
                //Console.WriteLine(string.Join(",",indices));

                int vboID = GL.GenBuffer();
                vbos[0] = vboID;
                GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
                float[] _vertices = new float[vertices.Length * 3];
                for(int i = 0; i < vertices.Length; i++)
                {
                    _vertices[(i * 3) + 0] = map.Vertices[vertices[i]].X;
                    _vertices[(i * 3) + 1] = map.Vertices[vertices[i]].Y;
                    _vertices[(i * 3) + 2] = -7.5f;
                }
                //Console.WriteLine(string.Join(",", _vertices));

                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(_vertices.Length * sizeof(float)), _vertices, BufferUsageHint.StaticDraw);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

                // Indices
                vboID = GL.GenBuffer();
                vbos[1] = vboID;
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboID);
                GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(_indices.Length * sizeof(uint)), _indices, BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }

            public int GetVaoID()
            {
                return vaoID;
            }

            public void Destroy()
            {
                for(int i = 0; i < vbos.Length; i++)
                {
                    GL.DeleteVertexArray(vbos[i]);
                }
            }
        }

        /*
        public class Vertex
        {
            public float x;
            public float y;

            public Vertex()
            {
                x = 0.0f;
                y = 0.0f;
            }

            public Vertex(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }
        */

        public class Line
        {
            public int a;
            public int b;

            public int sector;

            public Line()
            {
                this.a = -1;
                this.b = -1;
                sector = -1;
            }

            public Line(int a, int b)
            {
                this.a = a;
                this.b = b;
                sector = -1;
            }

            public void SetSector(int sector)
            {
                this.sector = sector;
            }
        }

        public class Thing
        {
            public double x, y, z;
            public int id;
            public double angle;

            public Thing()
            {
            }

            public Thing(double x, double y, double z, int id, double angle)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.id = id;
                this.angle = angle;
            }

            public override string ToString()
            {
                return "(" + x + "," + y + "," + z + ") (" + angle + ") " + id;
            }
        }

        private class Map
        {
            public List<Vector2> Vertices = new();
            public List<Line> Lines = new();
            public List<Sector> Sectors = new();
            public List<Thing> Things = new();
        }

        public class SerializedMap
        {
            public string MapName;
            public double[] Vertices;
            public int[] Lines;
            public Sector[] Sectors;
            public Thing[] things;

            public SerializedMap()
            { 
            }

            public SerializedMap(string mapName, Vector2[] Vertices, Line[] Lines, Sector[] Sectors, Thing[] things)
            {
                this.MapName = mapName;
                this.Vertices = new double[Vertices.Length * 2];
                for (int i = 0; i < Vertices.Length; i++)
                {
                    this.Vertices[(i * 2) + 0] = Vertices[i].X;
                    this.Vertices[(i * 2) + 1] =  Vertices[i].Y;
                }
                this.Lines = new int[Lines.Length * 2];
                for(int i = 0; i < Lines.Length; i++)
                {
                    this.Lines[(i*2)+0] = Lines[i].a;
                    this.Lines[(i*2)+1] = Lines[i].b;
                }
                this.Sectors = Sectors.ToArray();
                this.things = things.ToArray();
            }
        }

        static Map map;

        public GoonED() : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = new Vector2i(1600, 900), APIVersion = new Version(3, 3) })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            Title = "GoonED";
            VSync = VSyncMode.On;

            _ImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);

            map = new Map();
            // Add Mike
            map.Things.Add(new Thing(0, 10, 0, 13484, 0));
            /*
            map.Vertices.Add(new Vector2(5.0f, 1.0f));
            map.Vertices.Add(new Vector2(-5.0f, -1.0f));
            map.Lines.Add(new Line(0, 1));
            map.Sectors.Add(new Sector(new int[] { 0, 1 }, new int[] { 0 }, 10.0f, 5.0f, 0));
            */

            sectorRenderer = new SectorRenderer();
            spriteRenderer = new SpriteRenderer();

            Textures.Add("Unknown", new Texture("unknown.png"));
            Textures.Add("Mike", new Texture("mike.png"));
            Textures.Add("Goon", new Texture("goon.png"));
            Textures.Add("Mook", new Texture("mook.png"));
            Textures.Add("Wiseguy", new Texture("wiseguy.png"));

            byte[] nameStart = new UTF8Encoding(true).GetBytes("New Map");
            for(int i = 0; i < nameStart.Length; i++)
            {
                mapName[i] = nameStart[i];
            }

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
            map.Sectors.ForEach((s) =>
            {
                s.Destroy();
            });
            foreach(KeyValuePair<string, Texture> entry in Textures)
            {
                entry.Value.Cleanup();
            }
            sectorRenderer.Cleanup();
            spriteRenderer.Cleanup();
            _ImGuiController.Dispose();
            base.OnUnload();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if(shiftDown)
            {
                if (hoveredSector != -1)
                    map.Sectors[hoveredSector].hovered = false;
                hoveredSector = -1;
            }

            for (int i = 0; i < map.Sectors.Count; i++)
            {
                if (selectedSector != i || map.Sectors[i].layer != layer)
                {
                    map.Sectors[i].selected = false;
                }
                if (hoveredSector != i || map.Sectors[i].layer != layer)
                {
                    map.Sectors[i].hovered = false;
                }
            }
        }

        private bool inputConsumed = false;

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            _ImGuiController.Update(this, (float)(args.Time), out bool inputConsumed);
            this.inputConsumed = inputConsumed;

            Vector2 mousePos = _camera.GetRelativeMousePos(MousePosition);

            PointRenderer.BeginFrame();
            LineRenderer.BeginFrame();

            GL.ClearColor(new Color4(0, 0, 0, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            for (int i = (int)-_camera.orthographicSize; i < (int)_camera.orthographicSize; i++)
            {
                LineRenderer.AddLine(new Vector2(i + MathF.Round(_camera.position.X), -_camera.orthographicSize + _camera.position.Y),
                                        new Vector2(i + MathF.Round(_camera.position.X), _camera.orthographicSize + _camera.position.Y),
                    i + MathF.Round(_camera.position.X) == 0 ? new Vector3(0.3f, 1.0f, 0.3f) : new Vector3(0.5f, 0.5f, 0.5f), 1);

                LineRenderer.AddLine(new Vector2(-_camera.orthographicSize + _camera.position.X, i + MathF.Round(_camera.position.Y)),
                                        new Vector2(_camera.orthographicSize + _camera.position.X, i + MathF.Round(_camera.position.Y)),
                    i + MathF.Round(_camera.position.Y) == 0 ? new Vector3(1.0f, 0.3f, 0.3f) : new Vector3(0.5f, 0.5f, 0.5f), 1);
            }

            for (int i = (int)-_camera.orthographicSize; i < (int)_camera.orthographicSize; i++)
            {
                LineRenderer.AddLine(new Vector2(i + MathF.Round(_camera.position.X), -_camera.orthographicSize + _camera.position.Y),
                                        new Vector2(i + MathF.Round(_camera.position.X), _camera.orthographicSize + _camera.position.Y),
                    i + MathF.Round(_camera.position.X) == 0 ? new Vector3(0.3f, 1.0f, 0.3f) : new Vector3(0.5f, 0.5f, 0.5f), 1);

                LineRenderer.AddLine(new Vector2(-_camera.orthographicSize + _camera.position.X, i + MathF.Round(_camera.position.Y)),
                                        new Vector2(_camera.orthographicSize + _camera.position.X, i + MathF.Round(_camera.position.Y)),
                    i + MathF.Round(_camera.position.Y) == 0 ? new Vector3(1.0f, 0.3f, 0.3f) : new Vector3(0.5f, 0.5f, 0.5f), 1);
            }

            /*
            Vector2 mousePos = _camera.GetRelativeMousePos(MousePosition);
            if (!inputConsumed)
            {
                LineRenderer.AddLine(new Vector2(0, 0), mousePos, YELLOW, 1);

                PointRenderer.AddPoint(mousePos, YELLOW, 1);
            }
            */

            DrawSectors();
            DrawLines();
            DrawVertices();

            //GL.Enable(EnableCap.DepthTest);
            LineRenderer.Draw(_camera);
            PointRenderer.Draw(_camera);
            sectorRenderer.Render(map.Sectors, _camera);

            List<Sprite> temp = new List<Sprite>();
            Textures.TryGetValue("Unknown", out Texture unknown);
            Textures.TryGetValue("Mike", out Texture mike);
            Textures.TryGetValue("Goon", out Texture goon);
            Textures.TryGetValue("Mook", out Texture mook);
            Textures.TryGetValue("Wiseguy", out Texture wiseguy);

            for(int i = 0; i < map.Things.Count; i++)
            {
                //Console.WriteLine(map.Things[i]);

                var tex = unknown;

                if (map.Things[i].id == 13484)
                    tex = mike;
                if (map.Things[i].id == 10001)
                    tex = goon;
                if (map.Things[i].id == 10002)
                    tex = mook;
                if (map.Things[i].id == 10003)
                    tex = wiseguy;

                temp.Add(new Sprite(new Vector2((float)map.Things[i].x, (float)map.Things[i].z), (float)map.Things[i].angle, tex)
                    .SetAlpha(selectedThing == i ? 1.0f : hoveredThing == i ? 0.8f : 0.6f));
            }

            if(shiftDown)
            {
                temp.Add(new Sprite(new Vector2(ctrlDown ? mousePos.X : MathF.Round(mousePos.X), ctrlDown ? mousePos.Y : MathF.Round(mousePos.Y)), 0.0f, goon).SetAlpha(0.2f));
            }

            //temp.Add(new Sprite(new Vector2(0, 0), 0.0f, mike));
            //temp.Add(new Sprite(new Vector2(4, 2), 0.0f, mike).SetAlpha(0.2f));
            //temp.Add(new Sprite(new Vector2(-3, -2), 35.0f, mike));

            spriteRenderer.Render(temp, _camera);
            //GL.Disable(EnableCap.DepthTest);

            //ImGui.DockSpaceOverViewport();

            if (AcesDataPath != null && !loadPrompt)
            {
                ImGui.Begin("Editor");

                ImGui.TextUnformatted("FPS: " + (int)(1.0f / args.Time));
                ImGui.TextUnformatted("Camera Position: " + _camera.position.ToString("0.##"));
                ImGui.TextUnformatted("Mouse Position: " + MousePosition.ToString("0.##"));
                ImGui.TextUnformatted("Global Mouse Position: " + _camera.GetRelativeMousePos(MousePosition).ToString("0.##"));

                ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                Vector2f buttonSize = new Vector2f(75.0f, 0.0f);
                ImGui.TextUnformatted("Map Name");
                ImGui.InputText("##MapName", mapName, 50);
                ImGui.Dummy(new Vector2f(0.0f, 10.0f));

                if (ConvexPolyAlertTimer > 0.0f)
                {
                    ImGui.TextColored(new Vector4f(1.0f, 0.2f, 0.2f, 1.0f), "Sectors cannot be concave!");
                    ConvexPolyAlertTimer -= (float)args.Time;
                }

                if (ImGui.Button("Save", buttonSize))
                {
                    Save();
                }
                if (ImGui.Button("Load", buttonSize))
                {
                    loadPrompt = true;
                }
                if (ImGui.Button("Export", buttonSize))
                {
                    Export();
                }
                ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                ImGui.TextUnformatted("Layer");
                if (ImGui.Button("-")) { layer--; }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(20);
                ImGui.DragInt("##", ref layer, 0.08f, 0, 99);
                ImGui.SameLine();
                if (ImGui.Button("+")) { layer++; }
                if (layer < 0)
                {
                    layer = 0;
                }
                if (layer > 99)
                {
                    layer = 99;
                }

                if(selectedSector != -1)
                {
                    map.Sectors[selectedSector].selected = true;
                    //map.Sectors[selectedSector].hovered = false;

                    ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                    ImGui.TextUnformatted("Sector Properties");
                    ImGui.Dummy(new Vector2f(0.0f, 5.0f));

                    ImGui.TextUnformatted("Floor Height");
                    float floor = (float)map.Sectors[selectedSector].floor;
                    ImGui.DragFloat("##Floor", ref floor, 0.01f);
                    map.Sectors[selectedSector].floor = floor;
                    ImGui.TextUnformatted("Ceiling Height");
                    float ceil = (float)map.Sectors[selectedSector].ceiling;
                    ImGui.DragFloat("##Ceil", ref ceil, 0.01f);
                    map.Sectors[selectedSector].ceiling = ceil;
                }

                if(selectedThing != -1)
                {
                    ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                    ImGui.TextUnformatted("Thing Properties");
                    ImGui.Dummy(new Vector2f(0.0f, 5.0f));
                    float _temp = 0.0f;
                    _temp = (float)map.Things[selectedThing].x;
                    ImGui.DragFloat("X Pos", ref _temp, 0.01f);
                    map.Things[selectedThing].x = _temp;
                    _temp = (float)map.Things[selectedThing].y;
                    ImGui.DragFloat("Y Pos", ref _temp, 0.01f);
                    map.Things[selectedThing].y = _temp;
                    _temp = (float)map.Things[selectedThing].z;
                    ImGui.SetItemTooltip("This controls the Y position in 3d space, you won't be able to see it in the editor");
                    ImGui.DragFloat("Z Pos", ref _temp, 0.01f);
                    map.Things[selectedThing].z = _temp;

                    ImGui.Dummy(new Vector2f(0.0f, 5.0f));
                    _temp = (float)map.Things[selectedThing].angle;
                    ImGui.DragFloat("Angle", ref _temp, 0.1f);
                    map.Things[selectedThing].angle = _temp;
                    ImGui.Dummy(new Vector2f(0.0f, 5.0f));
                    ImGui.InputInt("Thing ID", ref map.Things[selectedThing].id);
                    ImGui.TextUnformatted("Thing ID Presets");
                    if (ImGui.Button("Mike")) { map.Things[selectedThing].id = 13484; }
                    if (ImGui.Button("Goon")) { map.Things[selectedThing].id = 10001; }
                    if (ImGui.Button("Mook")) { map.Things[selectedThing].id = 10002; }
                    if (ImGui.Button("Wiseguy")) { map.Things[selectedThing].id = 10003; }
                }
            }

            if (AcesDataPath == null)
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
                ImGui.SetNextWindowSize(new Vector2f(ClientSize.X + 5, ClientSize.Y + 5));
                ImGui.Begin("NoInteractPanel", window_flags);
                ImGui.End();

                ImGui.Begin("Setup");
                ImGui.Text("Welcome to GoonED");
                ImGui.Text("Read the README for instructions");
                ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                ImGui.Text("Enter the path to your AcesData folder.");
                ImGui.InputText("###", TempAcesPath, 256);
                if (ImGui.Button("Confirm"))
                {
                    AcesDataPath = Encoding.Default.GetString(TempAcesPath).Replace("\0", string.Empty).Trim();

                    if (!Directory.Exists(AcesDataPath) || !AcesDataPath.EndsWith("AcesData") || !File.Exists(AcesDataPath + "/../Fallen Aces.exe"))
                    {
                        AcesDataPathAlertTimer = 5.0f;
                        AcesDataPath = null;
                    }
                    else
                    {
                        Console.WriteLine("Aces Data path saved as: " + AcesDataPath);

                        FileStream file = File.Create("user.ini");
                        file.Write(TempAcesPath);
                        file.Close();
                    }
                }
                if (AcesDataPathAlertTimer > 0.0f)
                {
                    ImGui.TextColored(new Vector4f(1.0f, 0.2f, 0.2f, 1.0f), "Invalid Path!");
                    AcesDataPathAlertTimer -= (float)args.Time;
                }
                ImGui.End();
            }

            if (loadPrompt)
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
                ImGui.SetNextWindowSize(new Vector2f(ClientSize.X + 5, ClientSize.Y + 5));
                ImGui.Begin("NoInteractPanel", window_flags);
                ImGui.End();

                ImGui.Begin("Load Map");
                ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                ImGui.Text("Enter the name of your map save file");
                ImGui.InputText("###LoadMapName", LoadMapName, 256);
                if (ImGui.Button("Confirm"))
                {
                    string mapName = Encoding.Default.GetString(LoadMapName).Replace("\0", string.Empty).Trim();

                    if (!File.Exists("saves/" + mapName + ".goon"))
                    {
                        AcesDataPathAlertTimer = 5.0f;
                    }
                    else
                    {
                        string data = File.ReadAllText("saves/" + mapName + ".goon");
                        LoadMap(data);
                        loadPrompt = false;
                    }
                }

                ImGui.SameLine();
                if(ImGui.Button("Cancel"))
                {
                    loadPrompt = false;
                }

                if (AcesDataPathAlertTimer > 0.0f)
                {
                    ImGui.TextColored(new Vector4f(1.0f, 0.2f, 0.2f, 1.0f), "Invalid Path!");
                    AcesDataPathAlertTimer -= (float)args.Time;
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

        private void Save()
        {
            if (!Directory.Exists("saves"))
                Directory.CreateDirectory("saves");

            string _mapName = Encoding.Default.GetString(mapName).Replace("\0", string.Empty).Trim();

            SerializedMap serializedMap = new SerializedMap(_mapName, map.Vertices.ToArray(), map.Lines.ToArray(), map.Sectors.ToArray(), map.Things.ToArray());
            string _map = JsonNet.Serialize(serializedMap);

            FileStream file = File.Create("saves/" + _mapName + ".goon");
            file.Write(Encoding.ASCII.GetBytes(_map));
            file.Close();
        }

        private void LoadMap(string data)
        {
            map.Vertices.Clear();
            map.Lines.Clear();
            map.Sectors.Clear();
            map.Things.Clear();

            SerializedMap jsonMap = JsonNet.Deserialize<SerializedMap>(data);

            for (int i = 0; i < jsonMap.Vertices.Length; i+=2)
            {
                map.Vertices.Add(new Vector2((float)jsonMap.Vertices[i+0], (float)jsonMap.Vertices[i+1]));
            }

            for (int i = 0; i < jsonMap.Lines.Length; i+=2)
            {
                map.Lines.Add(new Line(jsonMap.Lines[i+0], jsonMap.Lines[i+1]));
            }

            for(int i = 0; i < jsonMap.Sectors.Length; i++)
            {
                var sector = jsonMap.Sectors[i];
                map.Sectors.Add(new Sector(sector.vertices, sector.lines, sector.floor, sector.ceiling, sector.layer));
            }

            for(int i = 0; i < jsonMap.things.Length; i++)
            {
                var thing = jsonMap.things[i];
                Console.Write(thing);
                map.Things.Add(thing);
            }

            this.mapName = Encoding.UTF8.GetBytes(jsonMap.MapName);
        }

        float AcesDataPathAlertTimer = 0.0f;
        float ConvexPolyAlertTimer = 0.0f;

        private void DrawVertices()
        {
            if (startEditIndex_Vertex < 0) return;

            for (int i = startEditIndex_Vertex; i < map.Vertices.Count; i++)
            {
                if (i != startEditIndex_Vertex)
                    PointRenderer.AddPoint(new Vector2(map.Vertices[i].X, map.Vertices[i].Y), YELLOW, 1);
            }

            PointRenderer.AddPoint(new Vector2(map.Vertices[startEditIndex_Vertex].X, map.Vertices[startEditIndex_Vertex].Y), RED, 1);
        }

        private void DrawLines()
        {
            if (startEditIndex_Line < 0) return;

            for (int i = startEditIndex_Line; i < map.Lines.Count; i++)
            {
                LineRenderer.AddLine(map.Vertices[map.Lines[i].a], map.Vertices[map.Lines[i].b], YELLOW, 1);
            }
        }

        private void DrawSectors()
        {
            for (int i = 0; i < map.Sectors.Count; i++)
            {
                Vector3 col = new Vector3(1.0f, 1.0f, 0.0f);

                if (map.Sectors[i].layer != layer) col *= 0.2f;

                for (int j = 0; j < map.Sectors[i].lines.Length; j++)
                {
                    LineRenderer.AddLine(new Vector2(map.Vertices[map.Sectors[i].lines[j]].X, map.Vertices[map.Sectors[i].lines[j]].Y),
                        new Vector2(map.Vertices[map.Sectors[i].lines[(j + 1) % map.Sectors[i].lines.Length]].X,
                        map.Vertices[map.Sectors[i].lines[(j + 1) % map.Sectors[i].lines.Length]].Y),
                        col, 1);
                }

                for (int j = 0; j < map.Sectors[i].vertices.Length; j++)
                {
                    PointRenderer.AddPoint(new Vector2(map.Vertices[map.Sectors[i].vertices[j]].X, map.Vertices[map.Sectors[i].vertices[j]].Y), col, 1);
                }
            }
        }

        private void Export()
        {
            var vertices = map.Vertices;
            var lines = map.Lines;
            var sectors = map.Sectors;
            var things = map.Things;

            StringBuilder mapFile = new StringBuilder();
            StringBuilder infoFile = new StringBuilder();

            // WRITE MAP DATA
            mapFile.AppendLine("Global");
            mapFile.AppendLine("{");
            mapFile.AppendLine("editor_version_major = 0;");
            mapFile.AppendLine("editor_version_minor = 7;");
            mapFile.AppendLine("editor_version_revision = 7;");
            mapFile.AppendLine("map_version_major = 1;");
            mapFile.AppendLine("map_version_minor = 7;");
            mapFile.AppendLine("acesdata_version = 14;");
            mapFile.AppendLine("default_texture_scale = 0.5;");
            mapFile.AppendLine("skybox = 3;");
            mapFile.AppendLine("skybox_rotation = 0;");
            mapFile.AppendLine("skybox_height_offset = 0.4;");
            mapFile.AppendLine("fog_density = 0.0075;");
            mapFile.AppendLine("fog_color = 0.6737489, 0.7926024, 0.9900501, 1;");
            mapFile.AppendLine("weather = 0;");
            mapFile.AppendLine("backup_index = 2;");
            mapFile.AppendLine("}");
            mapFile.AppendLine();

            // WRITE LAYERS
            for (int i = 0; i < 10; i++)
            {
                mapFile.AppendLine("");
                mapFile.AppendLine("LayerInfo");
                mapFile.AppendLine("{");
                mapFile.AppendLine("id = " + i + ";");
                mapFile.AppendLine(@"name = Layer """ + i + @""";");
                mapFile.AppendLine("}");
                mapFile.AppendLine();
            }

            // WRITE VERTICES
            for (int i = 0; i < vertices.Count; i++)
            {
                mapFile.AppendLine("Vertex // " + i);
                mapFile.AppendLine("{");
                mapFile.AppendLine("x = " + (vertices[i].X) + ";");
                mapFile.AppendLine("z = " + (vertices[i].Y) + ";");
                mapFile.AppendLine("}");
                mapFile.AppendLine();
            }

            // WRITE LINES
            for (int i = 0; i < lines.Count; i++)
            {
                mapFile.AppendLine("Line // " + i);
                mapFile.AppendLine("{");
                mapFile.AppendLine("v1 = " + (lines[i].a) + ";");
                mapFile.AppendLine("v2 = " + (lines[i].b) + ";");
                mapFile.AppendLine("}");
                mapFile.AppendLine();
            }

            // WRITE SIDE DEFS
            for (int i = 0; i < lines.Count; i++)
            {
                mapFile.AppendLine("Side // " + i);
                mapFile.AppendLine("{");
                mapFile.AppendLine("lines = " + i + ";");
                mapFile.AppendLine("sector = " + (lines[i].sector) + ";");
                mapFile.AppendLine("side_plane( );");
                mapFile.AppendLine(@"side_texture( path = ""Editor/Default.png""; scale = 1,1; )");
                mapFile.AppendLine("}");
                mapFile.AppendLine();
            }

            // WRITE SECTORS
            for (int i = 0; i < sectors.Count; i++)
            {
                mapFile.AppendLine("Sector // " + i);
                mapFile.AppendLine("{");
                mapFile.AppendLine("layer = " + sectors[i].layer + ";");

                var verticesText = "";
                for (int j = 0; j < sectors[i].vertices.Length; j++)
                {
                    verticesText += sectors[i].vertices[j] + ",";
                }

                mapFile.AppendLine("vertices = " + verticesText + ";");

                var lineText = "";
                for (int j = 0; j < sectors[i].vertices.Length; j++)
                {
                    lineText += sectors[i].lines[j] + ",";
                }

                mapFile.AppendLine("lines = " + lineText + ";");

                mapFile.AppendLine("height_floor = " + sectors[i].floor + ";");
                mapFile.AppendLine("height_ceiling = " + sectors[i].ceiling + ";");
                mapFile.AppendLine("lighting = 1, 1, 1, 1;");
                mapFile.AppendLine("floor_slope ( sloped = False; direction = 0; height = 0; )");
                mapFile.AppendLine("ceiling_slope( sloped = False; direction = 0; height = 0; )");
                mapFile.AppendLine(@"floor_texture(path = ""Editor/Default.png""; offset = 0, 0; scale = 0.5, 0.5; angle = 0; )");
                mapFile.AppendLine(@"ceiling_texture(path = ""Editor/Default.png""; offset = 0, 0; scale = 0.5, 0.5; angle = 0; )");
                mapFile.AppendLine(@"floor_plane(visible = True; solid = True; brightness_offset = 0; )");
                mapFile.AppendLine(@"ceiling_plane(visible = True; solid = True; brightness_offset = 0; )");

                mapFile.AppendLine("}");
                mapFile.AppendLine();
            }

            // WRITE THINGS (why are they called just things again)
            for (int i = 0; i < things.Count; i++)
            {
                mapFile.AppendLine("Thing // " + i);
                mapFile.AppendLine("{");
                mapFile.AppendLine("layer = 0;");
                mapFile.AppendLine("x = " + things[i].x + ";");
                mapFile.AppendLine("y = " + things[i].y + ";");
                mapFile.AppendLine("z = " + things[i].z + ";");
                mapFile.AppendLine("definition_id = " + things[i].id + ";");
                mapFile.AppendLine("angle = " + (things[i].angle + 90) + ";");
                mapFile.AppendLine("height = 0;");
                mapFile.AppendLine("height_mode = 0;");

                if (things[i].id > 10000 && things[i].id < 10017)
                {
                    mapFile.AppendLine("enemy ( )");
                }

                mapFile.AppendLine("}");
                mapFile.AppendLine();
            }

            // WRITE CHAPTER INFO TO SEPERATE FILE
            string _mapName = Encoding.Default.GetString(mapName).Replace("\0", string.Empty).Trim();

            infoFile.AppendLine(@"title = """ + _mapName + @""";");
            infoFile.AppendLine(@"over_title_text = ""Custom Level"";");
            infoFile.AppendLine(@"order = 99;");
            infoFile.AppendLine(@"secret_count = 0;");
            infoFile.AppendLine(@"loading_screen_ambience = 9;");
            infoFile.AppendLine(@"loading_screen_music = 27;");
            infoFile.AppendLine(@"world_file_name = """ + Regex.Replace(_mapName, @"\s+", "") + @".txt"";");
            infoFile.AppendLine(@"sprite_groups = ""Level 1"", ""Note Backgrounds"";");
            infoFile.AppendLine(@"always_unlocked = true;");
            infoFile.AppendLine(@"description_text = ""Custom Level Description"";");
            infoFile.AppendLine(@"faction_name = 0 ""Glasshearts"";");
            infoFile.AppendLine(@"faction_color = 0 ""red"";");
            infoFile.AppendLine(@"faction_name = 1 ""Benedettos"";");
            infoFile.AppendLine(@"faction_color = 1 ""purple"";");
            infoFile.AppendLine();

            if (!Directory.Exists(_mapName))
                Directory.CreateDirectory(_mapName);

            using (FileStream file = File.Create(_mapName + "/" + Regex.Replace(_mapName, @"\s+", "") + ".txt"))
            {
                byte[] data = new UTF8Encoding(true).GetBytes(mapFile.ToString());
                file.Write(data, 0, data.Length);
                file.Close();
            }
            using (FileStream file = File.Create(_mapName + "/chapterInfo.txt"))
            {
                byte[] data = new UTF8Encoding(true).GetBytes(infoFile.ToString());
                file.Write(data, 0, data.Length);
                file.Close();
            }
            using (FileStream file = File.Create(_mapName + "/loading.jpg"))
            {
                byte[] data = FileTools.ReadAsBytes("loading.jpg");
                file.Write(data, 0, data.Length);
                file.Close();
            }
            using (FileStream file = File.Create(_mapName + "/poster.png"))
            {
                byte[] data = FileTools.ReadAsBytes("poster.png");
                file.Write(data, 0, data.Length);
                file.Close();
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            _ImGuiController.PressChar((char)e.Unicode);
        }

        const float zoomSpeed = 1.2f;

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _ImGuiController.MouseScroll(e.Offset);

            if(!inputConsumed)
            {
                if (e.OffsetY > 0 && _camera.orthographicSize > 0.55f)
                    _camera.orthographicSize /= zoomSpeed;
                else if (_camera.orthographicSize < 54.0f)
                    _camera.orthographicSize *= zoomSpeed;

                _camera.RefreshMatrix(ClientSize.X, ClientSize.Y);
            }
        }

        Vector2 dragOrigin = Vector2.Zero;
        int hoveredSector = -1;
        int selectedSector = -1;

        int hoveredThing = -1;
        int selectedThing = -1;

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            Vector2 cameraPos = _camera.GetRelativeMousePos(MousePosition);

            if (middleMouse)
            {
                Vector2 difference = cameraPos - _camera.position;
                Vector2 targetPosition = dragOrigin - difference;

                _camera.position.X = targetPosition.X;
                _camera.position.Y = targetPosition.Y;
            }

            if(!shiftDown)
            {
                hoveredThing = -1;
                bool sectorHovered = false;
                for (int i = 0; i < map.Sectors.Count; i++)
                {
                    if (map.Sectors[i].layer == layer && PointInSector(cameraPos.X, cameraPos.Y, map.Sectors[i]))
                    {
                        sectorHovered = true;
                        hoveredSector = i;
                        map.Sectors[hoveredSector].hovered = true;
                        break;
                    }
                }

                if (!sectorHovered)
                {
                    if (hoveredSector != -1 && hoveredSector < map.Sectors.Count)
                    {
                        map.Sectors[hoveredSector].hovered = false;
                    }
                    hoveredSector = -1;
                }
            }
            else
            {
                hoveredThing = -1;
                Vector2 mPos = _camera.GetRelativeMousePos(MousePosition);
                for (int i = 0; i < map.Things.Count; i++)
                {
                    Console.WriteLine(Vector2.Distance(mPos, new Vector2((float)map.Things[i].x, (float)map.Things[i].z)));
                    if (Vector2.Distance(mPos, new Vector2((float)map.Things[i].x, (float)map.Things[i].z)) < 1.0f)
                    {
                        hoveredThing = i;
                        break;
                    }
                }
                hoveredSector = -1;
            }
        }

        private const float DIST_EPSILON = 0.01f;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (inputConsumed) return;

            if (e.Button == MouseButton.Left)
            {
                leftMouse = true;
                if(!shiftDown)
                {
                    selectedThing = -1;
                    if (hoveredSector >= 0)
                    {
                        if (selectedSector != -1)
                            map.Sectors[selectedSector].selected = false;
                        selectedSector = hoveredSector;
                    }
                    else
                    {
                        if (selectedSector != -1)
                            map.Sectors[selectedSector].selected = false;
                        selectedSector = -1;
                    }
                }
                else
                {
                    if (selectedSector != -1)
                        map.Sectors[selectedSector].selected = false;
                    selectedSector = -1;
                    Vector2 mPos = _camera.GetRelativeMousePos(MousePosition);
                    selectedThing = -1;
                    for(int i = 0; i < map.Things.Count; i++)
                    {
                        Console.WriteLine(Vector2.Distance(mPos, new Vector2((float)map.Things[i].x, (float)map.Things[i].z)));
                        if(Vector2.Distance(mPos, new Vector2((float)map.Things[i].x, (float)map.Things[i].z)) < 1.0f)
                        {
                            selectedThing = i;
                            break;
                        }
                    }
                }
            }
            if (e.Button == MouseButton.Right)
            {
                if (!shiftDown)
                    BuildVertex(e);
                else
                    AddThing(e);
                rightMouse = true;
            }
            if (e.Button == MouseButton.Middle)
            {
                dragOrigin = _camera.GetRelativeMousePos(MousePosition);
                middleMouse = true;
            }
        }

        /// <summary>
        /// Credit: https://stackoverflow.com/questions/4243042/c-sharp-point-in-polygon
        /// </summary>
        private bool PointInSector(float X, float Y, Sector s)
        {
            Vector2[] vertices = new Vector2[s.vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = map.Vertices[s.vertices[i]];
            }

            bool result = false;
            int j = vertices.Length - 1;

            for(int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].Y < Y && vertices[j].Y >= Y ||
                    vertices[j].Y < Y && vertices[i].Y >= Y)
                {
                    if (vertices[i].X + (Y - vertices[i].Y) /
                        (vertices[j].Y - vertices[i].Y) *
                        (vertices[j].X - vertices[i].X) < X)
                        result = !result;
                }
                j = i;
            }

            return result;
        }

        public void AddThing(MouseButtonEventArgs e)
        {
            Vector2 mousePos = _camera.GetRelativeMousePos(MousePosition);

            Vector2 pos = new Vector2(ctrlDown ? mousePos.X : MathF.Round(mousePos.X), ctrlDown ? mousePos.Y : MathF.Round(mousePos.Y));

            for(int i = 0; i < map.Things.Count; i++)
            {
                if(Vector2.Distance(new Vector2((float)map.Things[i].x, (float)map.Things[i].y), pos) < DIST_EPSILON) return;
            }

            map.Things.Add(new Thing(pos.X, 10, pos.Y, 10001, 0.0f));
        }

        private float CrossProductLength(float Ax, float Ay, float Bx, float By, float Cx, float Cy)
        {
            // Get the vectors' coordinates.
            float BAx = Ax - Bx;
            float BAy = Ay - By;
            float BCx = Cx - Bx;
            float BCy = Cy - By;

            // Calculate the Z coordinate of the cross product.
            return (BAx * BCy - BAy * BCx);
        }

        private void BuildVertex(MouseButtonEventArgs e) {
            if (startEditIndex_Vertex != -1)
            {
                map.Lines.Add(new Line(map.Vertices.Count - 1, map.Vertices.Count));
            }

            if (startEditIndex_Vertex == -1)
            {
                startEditIndex_Vertex = map.Vertices.Count;
                startEditIndex_Line = map.Lines.Count;
            }

            Vector2 mousePos = _camera.GetRelativeMousePos(MousePosition);

            map.Vertices.Add(new Vector2(MathF.Round(mousePos.X), MathF.Round(mousePos.Y)));

            for (int i = startEditIndex_Vertex; i < map.Vertices.Count - 1; i++)
            {
                if (Math.Max(map.Vertices[i].X, map.Vertices[map.Vertices.Count - 1].X) - Math.Min(map.Vertices[i].X, map.Vertices[map.Vertices.Count - 1].X) < DIST_EPSILON
                    && Math.Max(map.Vertices[i].Y, map.Vertices[map.Vertices.Count - 1].Y) - Math.Min(map.Vertices[i].Y, map.Vertices[map.Vertices.Count - 1].Y) < DIST_EPSILON)
                {
                    if (map.Vertices.Count - startEditIndex_Vertex <= 3)
                    {
                        map.Vertices.RemoveRange(map.Vertices.Count - 2, 2);
                        map.Lines.RemoveAt(map.Lines.Count - 1);
                        startEditIndex_Vertex = -1;
                        startEditIndex_Line = -1;
                        break;
                    }

                    map.Vertices.RemoveAt(map.Vertices.Count - 1);

                    List<int> vertexIndicesList = new();
                    for (int j = startEditIndex_Vertex; j < map.Vertices.Count; j++)
                    {
                        vertexIndicesList.Add(j);
                    }

                    Vector2 p1 = map.Vertices[vertexIndicesList[0]];
                    Vector2 p2 = map.Vertices[vertexIndicesList[1]];
                    Vector2 p3 = map.Vertices[vertexIndicesList[2]];
                    float val = (p2.Y - p1.Y) * (p3.X - p2.X) -
                        (p2.X - p1.X) * (p3.Y - p2.Y);

                    if (val < 0.0f)
                        vertexIndicesList.Reverse();

                    int[] vertexIndices = vertexIndicesList.ToArray();

                    List<int> lineIndicesList = new();
                    for (int j = startEditIndex_Line; j < map.Lines.Count; ++j)
                    {
                        lineIndicesList.Add(j);
                    }

                    int[] lineIndices = lineIndicesList.ToArray();

                    List<Vector2> temp_vertices = new();
                    for (int j = 0; j < vertexIndices.Length; j++)
                    {
                        temp_vertices.Add(map.Vertices[vertexIndices[j]]);
                    }
                    bool convex = !IsConcave(temp_vertices);

                    if (convex)
                        map.Sectors.Add(new Sector(vertexIndices, lineIndices, 10.0f, 0.0f, layer));
                    else
                    {
                        int difference = map.Vertices.Count - startEditIndex_Vertex;
                        for (int j = 0; j < difference; j++)
                        {
                            map.Vertices.RemoveAt(map.Vertices.Count - 1);
                        }
                        difference = map.Lines.Count - startEditIndex_Line;
                        for (int j = 0; j < difference; j++)
                        {
                            map.Lines.RemoveAt(map.Lines.Count - 1);
                        }

                        ConvexPolyAlertTimer = 10.0f;
                    }

                    startEditIndex_Vertex = -1;
                    startEditIndex_Line = -1;

                    break;
                }
            }
        }

        private bool IsConcave(List<Vector2> v)
        {
            List<Vector3> vertices = new();

            v.ForEach((vertex) =>
            {
                vertices.Add(new Vector3(vertex.X, vertex.Y, 0));
            });

            if (v.Count > 3)
            {
                Vector3 polyNormal = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);

                vertices.Add(vertices[0]);
                vertices.Add(vertices[1]);

                float direction = 0f;

                for (int i = 1; i < vertices.Count - 1; i++)
                {
                    Vector3 normal = Vector3.Cross(polyNormal, vertices[i] - vertices[i - 1]);

                    if (i != 1)
                    {
                        if (direction != MathF.Sign(Vector3.Dot(normal, vertices[i + 1] - vertices[i])))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        direction = MathF.Sign(Vector3.Dot(normal, vertices[i + 1] - vertices[i]));
                    }
                }
                return false;
            }
            return false;
        }

        private bool shiftDown = false;
        private bool ctrlDown = false;

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if(e.Key == Keys.Z)
            {
                if (!shiftDown)
                {
                    if (map.Sectors.Count - 1 >= 0)
                    {
                        Sector s = map.Sectors[map.Sectors.Count - 1];
                        map.Vertices.RemoveRange(map.Vertices.Count - s.vertices.Length, s.vertices.Length);
                        map.Lines.RemoveRange(map.Lines.Count - s.lines.Length, s.lines.Length);
                        s.Destroy();
                        map.Sectors.Remove(s);
                    }
                }
                else
                {
                    if(map.Things.Count > 1)
                        map.Things.RemoveAt(map.Things.Count - 1);
                }
            }

            if(e.Key == Keys.LeftShift || e.Key == Keys.RightShift)
            {
                shiftDown = true;
            }

            if (e.Key == Keys.LeftControl || e.Key == Keys.RightControl)
            {
                ctrlDown = true;
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Key == Keys.LeftShift || e.Key == Keys.RightShift)
            {
                shiftDown = false;
            }

            if (e.Key == Keys.LeftControl || e.Key == Keys.RightControl)
            {
                ctrlDown = false;
            }
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
