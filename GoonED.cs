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

namespace GoonED
{
    public class GoonED : GameWindow
    {
        private ImGuiController _ImGuiController;

        private Vector3 RED = new Vector3(1.0f, 0.0f, 0.0f);
        private Vector3 YELLOW = new Vector3(1.0f, 1.0f, 0.0f);

        // Map parameters
        private byte[] mapName = new byte[32];
        private Vector3f fogColor = new Vector3f(0, 0, 0);
        private int layer = 0;

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

        struct Thing
        {
            public float x, y, z;
            public int id;
            public float angle;

            public Thing(float x, float y, float z, int id, float angle)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.id = id;
                this.angle = angle;
            }
        }

        private class Map
        {
            public List<Vector2> Vertices = new();
            public List<Line> Lines = new();
            public List<Sector> Sectors = new();
            public List<Thing> Things = new();
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

        Map map;

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
            map.Things.Add(new Thing(0, 10, 0, 13484, 0));
            /*
            map.Vertices.Add(new Vector2(5.0f, 1.0f));
            map.Vertices.Add(new Vector2(-5.0f, -1.0f));
            map.Lines.Add(new Line(0, 1));
            map.Sectors.Add(new Sector(new int[] { 0, 1 }, new int[] { 0 }, 10.0f, 5.0f, 0));
            */

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
        }

        private bool inputConsumed = false;

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            _ImGuiController.Update(this, (float)(args.Time), out bool inputConsumed);
            this.inputConsumed = inputConsumed;

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

                    SerializedMap serializedMap = new SerializedMap(map.Vertices, map.Lines, map.Sectors);
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
                    Export();
                }
                ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                ImGui.TextUnformatted("Layer");
                if (ImGui.Button("-")) { layer--; }
                ImGui.SameLine();
                ImGui.DragInt("##", ref layer, 0.2f, 0, 99);
                ImGui.SameLine();
                if (ImGui.Button("+")) { layer++; }
                if(layer < 0)
                {
                    layer = 0;
                }

                if (layer > 99)
                {
                    layer = 99;
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
                ImGui.Text("Welcome to GoonED");
                ImGui.Text("Read the README for instructions");
                ImGui.Dummy(new Vector2f(0.0f, 10.0f));
                ImGui.Text("Enter the path to your AcesData folder.");
                ImGui.InputText("###", TempAcesPath, 256);
                if(ImGui.Button("Confirm"))
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
                if(AcesDataPathAlertTimer > 0.0f)
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

        float AcesDataPathAlertTimer = 0.0f;

        private void DrawVertices()
        {
            if (startEditIndex_Vertex < 0) return;

            for(int i = startEditIndex_Vertex; i < map.Vertices.Count; i++)
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
            for(int i = 0; i < map.Sectors.Count; i++)
            {
                for(int j = 0; j < map.Sectors[i].lines.Length; j++)
                {
                    LineRenderer.AddLine(new Vector2(map.Vertices[map.Sectors[i].lines[j]].X, map.Vertices[map.Sectors[i].lines[j]].Y),
                        new Vector2(map.Vertices[map.Sectors[i].lines[(j + 1) % map.Sectors[i].lines.Length]].X, 
                        map.Vertices[map.Sectors[i].lines[(j + 1) % map.Sectors[i].lines.Length]].Y),
                        YELLOW, 1);
                }

                for(int j = 0; j < map.Sectors[i].vertices.Length; j++)
                {
                    PointRenderer.AddPoint(new Vector2(map.Vertices[map.Sectors[i].vertices[j]].X, map.Vertices[map.Sectors[i].vertices[j]].Y), YELLOW, 1);
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
            for(int i = 0; i < vertices.Count; i++)
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
            for(int i = 0; i < things.Count; i++)
            {
                mapFile.AppendLine("Thing // " + i);
                mapFile.AppendLine("{");
                mapFile.AppendLine("layer = 0;");
                mapFile.AppendLine("x = " + things[i].x + ";");
                mapFile.AppendLine("y = " + things[i].y + ";");
                mapFile.AppendLine("z = " + things[i].z + ";");
                mapFile.AppendLine("definition_id = " + things[i].id + ";");
                mapFile.AppendLine("angle = " + (-things[i].angle + 90) + ";");
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
            infoFile.AppendLine(@"world_file_name = """+ Regex.Replace(_mapName, @"\s+", "") + @".txt"";");
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
                file.Write(data,0,data.Length);
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

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _ImGuiController.MouseScroll(e.Offset);
        }

        Vector2 dragOrigin = Vector2.Zero;

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if(middleMouse)
            {
                Vector2 difference = _camera.GetRelativeMousePos(MousePosition) - _camera.position;
                Vector2 targetPosition = dragOrigin - difference;

                _camera.position.X = targetPosition.X;
                _camera.position.Y = targetPosition.Y;
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

                for(int i = startEditIndex_Vertex; i < map.Vertices.Count - 1; i++)
                {
                    if (Math.Max(map.Vertices[i].X, map.Vertices[map.Vertices.Count-1].X) - Math.Min(map.Vertices[i].X, map.Vertices[map.Vertices.Count - 1].X) < DIST_EPSILON
                        && Math.Max(map.Vertices[i].Y, map.Vertices[map.Vertices.Count - 1].Y) - Math.Min(map.Vertices[i].Y, map.Vertices[map.Vertices.Count - 1].Y) < DIST_EPSILON)
                    {
                        if(map.Vertices.Count - startEditIndex_Vertex <= 3)
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

                        if(val < 0.0f)
                            vertexIndicesList.Reverse();

                        int[] vertexIndices = vertexIndicesList.ToArray();

                        List<int> lineIndicesList = new();
                        for (int j = startEditIndex_Line; j < map.Lines.Count; ++j)
                        {
                            lineIndicesList.Add(j);
                        }

                        int[] lineIndices = lineIndicesList.ToArray();

                        map.Sectors.Add(new Sector(vertexIndices, lineIndices, 10.0f, 0.0f, layer));

                        startEditIndex_Vertex = -1;
                        startEditIndex_Line = -1;

                        break;
                    }
                }
            }
            if(e.Button == MouseButton.Right)
                rightMouse = true;
            if (e.Button == MouseButton.Middle)
            {
                dragOrigin = _camera.GetRelativeMousePos(MousePosition);
                middleMouse = true;
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
