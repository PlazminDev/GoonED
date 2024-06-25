using System.Numerics;

using static SDL2.Bindings.SDL;

namespace GoonED
{
    class Program
    {

        private static void Main(string[] args)
        {
            new Program().Run();
        }

        private bool Running = false;

        const int FPS = 60;
        const int frameDelay = 1000 / FPS;

        uint frameStart;
        uint frameTime;

        nint window; nint renderer;

        const int gridSize = 64;

        Vector2 camera = new Vector2();
        Vector2 mousePos = new Vector2();

        //ImGuiDevice device;

        public void Run()
        {
            Init();

            while (Running)
            {
                frameStart = SDL_GetTicks();

                HandleEvents();
                Update();
                Render();

                frameTime = SDL_GetTicks() - frameStart;

                if (frameDelay > frameTime)
                {
                    SDL_Delay(frameDelay - frameTime);
                }
            }

            Cleanup();
        }

        private void Init()
        {
            SDL_Init(SDL_INIT_EVERYTHING);

            window = SDL_CreateWindow("GoonED", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, 1280, 720, SDL_WindowFlags.SDL_WINDOW_SHOWN);
            renderer = SDL_CreateRenderer(window, -1, 0);

            SDL_SetRenderDrawBlendMode(renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);

            Running = true;
        }

        bool leftMouse = false, rightMouse = false;
        bool holdingLeftMouse = false, holdingRightMouse = false;

        private void HandleEvents()
        {
            SDL_Event currEvent;
            while (SDL_PollEvent(out currEvent) != 0)
            {
                switch (currEvent.type)
                {
                    case SDL_EventType.SDL_QUIT:
                        Running = false;
                        break;
                    case SDL_EventType.SDL_MOUSEMOTION:
                        SDL_GetMouseState(out int x, out int y);

                        if (rightMouse)
                        {
                            camera.X -= (mousePos.X - x);
                            camera.Y -= (mousePos.Y - y);
                        }

                        mousePos.X = x;
                        mousePos.Y = y;
                        break;
                    case SDL_EventType.SDL_MOUSEBUTTONDOWN:

                        if (currEvent.button.button == SDL_BUTTON_LEFT)
                        {
                            leftMouse = true;
                        }
                        else if (currEvent.button.button == SDL_BUTTON_RIGHT)
                        {
                            rightMouse = true;
                        }
                        break;
                    case SDL_EventType.SDL_MOUSEBUTTONUP:
                        if (currEvent.button.button == SDL_BUTTON_LEFT)
                        {
                            leftMouse = false;
                        }
                        else if (currEvent.button.button == SDL_BUTTON_RIGHT)
                        {
                            rightMouse = false;
                        }
                        break;
                    default:
                        break;
                }
            }

            if (leftMouse && !holdingLeftMouse)
            {
                holdingLeftMouse = true;

                if (true)
                {
                    if (startEditIndex_VERTEX != -1)
                    {
                        lines.Add(new Line(vertices.Count - 1, vertices.Count));
                    }

                    if (startEditIndex_VERTEX == -1)
                    {
                        startEditIndex_VERTEX = vertices.Count;
                        startEditIndex_LINE = lines.Count;
                    }

                    vertices.Add(new Vector2(MathF.Round((mousePos.X - camera.X) / gridSize) * gridSize, MathF.Round((mousePos.Y - camera.Y) / gridSize) * gridSize));

                    for (int i = startEditIndex_VERTEX; i < vertices.Count - 1; i++)
                    {
                        if (vertices[i].X == vertices[vertices.Count - 1].X && vertices[i].Y == vertices[vertices.Count - 1].Y)
                        {
                            if (vertices.Count - startEditIndex_VERTEX <= 3)
                            {
                                vertices.RemoveRange(vertices.Count - 2, 2);
                                lines.RemoveRange(lines.Count - 1, 1);
                                startEditIndex_VERTEX = -1;
                                startEditIndex_LINE = -1;
                                break;
                            }

                            vertices.RemoveAt(vertices.Count - 1);

                            int[] vertexIndices = new int[vertices.Count - startEditIndex_VERTEX];
                            for (int j = startEditIndex_VERTEX, ind = 0; j < vertices.Count; j++, ind++) { vertexIndices[ind] = j; }

                            var p1 = vertices[vertexIndices[0]];
                            var p2 = vertices[vertexIndices[1]];
                            var p3 = vertices[vertexIndices[2]];
                            var val = (p2.Y - p1.Y) * (p3.X - p2.X) - (p2.X - p1.X) * (p3.Y - p2.Y);

                            if (val > 0) // Clockwise?
                                vertexIndices.Reverse();

                            Vector2 sectorCenter = new Vector2();
                            for (int j = 0; j < vertexIndices.Length; j++)
                            {
                                sectorCenter.X += vertices[vertexIndices[j]].X;
                                sectorCenter.Y += vertices[vertexIndices[j]].Y;
                            }

                            sectorCenter.X /= (float)vertexIndices.Length;
                            sectorCenter.Y /= (float)vertexIndices.Length;

                            int[] lineIndices = new int[lines.Count - startEditIndex_LINE];
                            for (int j = startEditIndex_LINE, ind = 0; j < lines.Count; j++, ind++)
                            {
                                lineIndices[ind] = j;
                            }

                            sectors.Add(new Sector(vertexIndices, lineIndices, 10, 5, 0));

                            for (int j = 0; j < lineIndices.Length; j++)
                                lines[lineIndices[j]].SetSector(sectors.Count - 1);

                            startEditIndex_VERTEX = -1;
                            startEditIndex_LINE = -1;

                            break;
                        }
                    }
                }
            }
            if (!leftMouse)
            {
                holdingLeftMouse = false;
            }
        }

        struct Sector
        {
            public int[] vertices;
            public int[] lines;
            public float floor;
            public float ceiling;
            public bool selected = false;
            public int layer;

            public Sector(int[] vertices, int[] lines, float floor, float ceiling, int layer)
            {
                this.vertices = vertices;
                this.lines = lines;
                this.floor = floor;
                this.ceiling = ceiling;
                this.layer = layer;
            }
        }

        struct Line
        {
            public int a { get; private set; }
            public int b { get; private set; }

            public int sector { get; private set; } = -1;

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

        List<Vector2> vertices = new List<Vector2>();
        List<Line> lines = new List<Line>();
        List<Sector> sectors = new List<Sector>();

        int startEditIndex_VERTEX = -1;
        int startEditIndex_LINE = -1;

        private void Update()
        {

        }

        private void Render()
        {
            SDL_RenderClear(renderer);

            // DRAW HERE
            /*
            SDL_SetRenderDrawColor(renderer, 255, 255, 0, 255);
            SDL_RenderDrawLine(renderer, 0, 0, (int)mousePos.X, (int)mousePos.Y);
            */
            DrawGrid();
            DrawSectors();
            DrawLines();
            DrawVertices();

            //DrawGUI();

            SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);

            SDL_RenderPresent(renderer);
        }

        #region Rendering Functions
        private void DrawLine(int x1, int y1, int x2, int y2)
        {
            SDL_RenderDrawLine(renderer, x1, y1, x2, y2);
        }

        private void DrawLine(float x1, float y1, float x2, float y2)
        {
            SDL_RenderDrawLine(renderer, (int)x1, (int)y1, (int)x2, (int)y2);
        }

        SDL_Rect rect = new SDL_Rect();

        private void DrawRect(float x, float y, float w, float h)
        {
            rect.x = (int)x;
            rect.y = (int)y;
            rect.w = (int)w;
            rect.h = (int)h;
            SDL_RenderFillRect(renderer, ref rect);
        }

        private void FillPolygon(Vector2[] points)
        {
            short[] xs = new short[points.Length];
            short[] ys = new short[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                xs[i] = (short)points[i].X;
                ys[i] = (short)points[i].Y;
            }

            SDL_Color color = new SDL_Color();
            color.r = 255;
            color.g = 255;
            color.b = 0;
            color.a = 255;
            //filledPolygonColor(renderer, xs, ys, points.Length, 0xFFFF00);
        }

        private void DrawSectors()
        {
            for (int i = 0; i < sectors.Count; i++)
            {
                SDL_SetRenderDrawColor(renderer, 255, 255, 0, 255);

                Vector2[] points = new Vector2[sectors[i].vertices.Length];
                for (int j = 0; j < sectors[i].lines.Length; j++)
                {
                    float x1 = vertices[sectors[i].lines[j]].X + camera.X;
                    float y1 = vertices[sectors[i].lines[j]].Y + camera.Y;

                    float x2 = vertices[sectors[i].lines[(j + 1) % sectors[i].lines.Length]].X + camera.X;
                    float y2 = vertices[sectors[i].lines[(j + 1) % sectors[i].lines.Length]].Y + camera.Y;

                    points[(j + 1) % sectors[i].vertices.Length] = new Vector2(x2, y2);

                    DrawLine(x1, y1, x2, y2);
                    DrawRect(x2 - 4, y2 - 4, 8, 8);
                }

                //FillPolygon(points);
            }
        }

        private void DrawVertices()
        {
            SDL_SetRenderDrawColor(renderer, 255, 255, 0, 255);

            if (startEditIndex_VERTEX == -1) return;

            for (int i = startEditIndex_VERTEX; i < vertices.Count; i++)
            {
                if (i != startEditIndex_VERTEX)
                {
                    DrawRect(vertices[i].X + camera.X - 4, vertices[i].Y + camera.Y - 4, 8, 8);
                }
            }

            DrawRect(vertices[startEditIndex_VERTEX].X + camera.X - 4, vertices[startEditIndex_VERTEX].Y + camera.Y - 4, 8, 8);
        }

        private void DrawLines()
        {
            SDL_SetRenderDrawColor(renderer, 255, 255, 0, 255);

            if (startEditIndex_LINE == -1) return;

            for (int i = startEditIndex_LINE; i < lines.Count; i++)
            {
                DrawLine(vertices[lines[i].a].X + camera.X, vertices[lines[i].a].Y + camera.Y, vertices[lines[i].b].X + camera.X, vertices[lines[i].b].Y + camera.Y);
            }
        }

        private void DrawGrid()
        {
            SDL_SetRenderDrawColor(renderer, 255, 255, 255, 128);

            SDL_GetWindowSize(window, out int w, out int h);

            for (int i = 0; i < w + gridSize; i += gridSize)
            {
                DrawLine((i + (camera.X % gridSize)), 0, (i + (camera.X % gridSize)), h);
            }

            for (int i = 0; i < h + gridSize; i += gridSize)
            {
                DrawLine(0, (i + (camera.Y % gridSize)), w, (i + (camera.Y % gridSize)));
            }
        }
        #endregion

        private void Cleanup()
        {
            SDL_DestroyWindow(window);
            SDL_DestroyRenderer(renderer);
            SDL_Quit();

            //ImGui.DestroyContext();

            /*
            if (!File.Exists("maps/MAP.txt"))
            {
                Directory.CreateDirectory("maps");
                File.WriteAllText("maps/MAP.txt", "Show me the champion of light\ni'll show you the herald of darkness");
            }
            */
        }
    }
}