var canvas, ctx;

var xPos = 0, yPos = 0; // Position of the camera
var pan = false;
var zoom = 1.00;

var gridSize = 64;

const Vector = (x, y) => ({ x, y }); // X: X position Y: Y position
const Line = (a, b) => ({ a, b }); // A: Index of first vertex B: Index of second vertex
const Sector = (vertices, lines, floor, ceiling, selected) => ({ vertices, lines, floor, ceiling, selected }); // A: Index of first vertex B: Index of second vertex

var vertices = [];
var lines = [];
var sectors = [];

const floorHeight = document.getElementById("floorHeight");
const ceilingHeight = document.getElementById("ceilingHeight");

var startEditIndex_Vertex = -1;
var startEditIndex_Line = -1;

var previousMousePos = Vector(0, 0);

document.addEventListener('contextmenu', event => event.preventDefault());

canvas = document.getElementById("canvas");

function save() {
    var savefile = "";

    savefile += "Global\n";
    savefile += "{\n";
    savefile += "editor_version_major = 0;\n";
    savefile += "editor_version_minor = 7;\n";
    savefile += "editor_version_revision = 7;\n";
    savefile += "map_version_major = 1;\n";
    savefile += "map_version_minor = 7;\n";
    savefile += "acesdata_version = 14;\n";
    savefile += "default_texture_scale = 0.5;\n";
    savefile += "skybox = 3;\n";
    savefile += "skybox_rotation = 0;\n";
    savefile += "skybox_height_offset = 0.4;\n";
    savefile += "fog_density = 0.0075;\n";
    savefile += "fog_color = 0.6737489, 0.7926024, 0.9900501, 1;\n";
    savefile += "weather = 0;\n";
    savefile += "backup_index = 2;\n";
    savefile += "}\n";

    savefile += "\n";
    savefile += "LayerInfo\n";
    savefile += "{\n";
    savefile += "id = 0;\n";
    savefile += 'name = "Default";\n';
    savefile += "}\n";
    savefile += "\n";
    
    savefile += "Event\n";
    savefile += "{\n";
    savefile += 'name = "None";\n';
    savefile += 'number = "0";\n';
    savefile += "}\n";
    savefile += "\n";

    for (let i = 0; i < vertices.length; i++) {
        savefile += "Vertex // " + i + "\n";
        savefile += "{\n";

        savefile += "x = " + (-vertices[i].x / gridSize) + ";\n";
        savefile += "z = " + (-vertices[i].y / gridSize) + ";\n";

        savefile += "}\n";
        savefile += "\n";
    }

    for (let i = 0; i < lines.length; i++) {
        savefile += "Line // " + i + "\n";
        savefile += "{\n";

        savefile += "v1 = " + lines[i].a + ";\n";
        savefile += "v2 = " + lines[i].b + ";\n";

        savefile += "}\n";
        savefile += "\n";
    }

    // for sidedefs
    for (let i = 0; i < lines.length; i++) {
        savefile += "Side // " + i + "\n";
        savefile += "{\n";

        savefile += "lines = " + i + ";\n";
        savefile += "sector = " + lines[i].sector + ";\n";
        savefile += "side_plane( )\n";
        savefile += 'side_texture ( path = "Editor/Default.png"; scale = 1,0.640625; )\n';

        savefile += "}\n";
        savefile += "\n";
    }

    for (let i = 0; i < sectors.length; i++) {
        savefile += "Sector // " + i + "\n";
        savefile += "{\n";
        savefile += "layer = 0\n";

        var verticesText = "";
        for (let j = 0; j < sectors[i].vertices.length; j++) {
            verticesText += sectors[i].vertices[j] + ",";
        }

        savefile += "vertices = " + verticesText + ";\n";

        var lineText = "";
        for (let j = 0; j < sectors[i].vertices.length; j++) {
            lineText += sectors[i].lines[j] + ",";
        }

        savefile += "lines = " + lineText + ";\n";

        savefile += "height_floor = " + sectors[i].floor + ";\n";
        savefile += "height_ceiling = " + sectors[i].ceiling + ";\n";
        savefile += "lighting = 1, 1, 1, 1;\n";
        savefile += "floor_slope ( sloped = False; direction = 0; height = 0; )\n";
        savefile += 'ceiling_slope( sloped = False; direction = 0; height = 0; )\n';
        savefile += 'floor_texture(path = "Editor/Default.png"; offset = 0, 0; scale = 0.5, 0.5; angle = 0; )\n';
        savefile += 'ceiling_texture(path = "Editor/Default.png"; offset = 0, 0; scale = 0.5, 0.5; angle = 0; )\n';
        savefile += "floor_plane(visible = True; solid = True; brightness_offset = 0; )\n";
        savefile += "ceiling_plane(visible = True; solid = True; brightness_offset = 0; )\n";

        savefile += "}\n";
        savefile += "\n";
    }

    savefile += "\n";

    savefile += "Thing // 0\n"
    savefile += "{\n";
    savefile += "layer = 0;\n";
    savefile += "x = 0;\n";
    savefile += "y = 15;\n";
    savefile += "z = 0;\n";
    savefile += "definition_id = 13484;\n";
    savefile += "angle = 90;\n";
    savefile += "height = 0;\n";
    savefile += "height_mode = 0;\n";
    savefile += "}\n";

    console.log(savefile);
    navigator.clipboard.writeText(savefile);
    alert("Map copied to clipboard");
}

function setupCanvas() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;

    xPos = canvas.width / 2;
    yPos = canvas.height / 2;

    ctx = canvas.getContext("2d");
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    setInterval(update, 1);
}

var holdingPlus = false, holdingMinus = false;

function update() {

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = "#000";
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // DRAW HERE

    drawGrid();

    drawLines();
    drawVertices();

    drawSectors();

    if (GetKey(Keys.Plus) && !holdingPlus) {
        gridSize *= 2;
        holdingPlus = true;
    } else if (!GetKey(Keys.Plus)) {
        holdingPlus = false;
    }

    if (GetKey(Keys.Minus) && !holdingMinus) {
        gridSize /= 2;
        holdingMinus = true;
    } else if (!GetKey(Keys.Minus)) {
        holdingMinus = false;
    }

    /*
    ctx.fillStyle = "#FFF";
    ctx.fillRect(xPos * zoom, yPos * zoom, 64 * zoom, 64 * zoom);
    */
}

function drawSectors() {
    for (let i = 0; i < sectors.length; i++) {
        ctx.beginPath();

        if (selectedIndex != i) {
            ctx.fillStyle = "#FFFF00";
        } else {
            ctx.fillStyle = "#0000FF";
        }

        if (i != hoveringIndex)
            ctx.globalAlpha = 0.2;
        else
            ctx.globalAlpha = 0.6;
        for (let j = 0; j < sectors[i].lines.length; j++) {
            var x = vertices[sectors[i].lines[j]].x + xPos;
            var y = vertices[sectors[i].lines[j]].y + yPos;

            if (j == 0)
                ctx.moveTo(x, y);
            else
                ctx.lineTo(x, y);
        }
        ctx.fill();
        ctx.globalAlpha = 1.0;

        for (let j = 0; j < sectors[i].vertices.length; j++) {
            ctx.fillRect(vertices[sectors[i].vertices[j]].x + xPos - 4, vertices[sectors[i].vertices[j]].y + yPos - 4, 8, 8);
        }

        ctx.beginPath();
        for (let j = 0; j < sectors[i].lines.length - 1; j++) {
            const a = lines[sectors[i].lines[j]].a;
            const b = lines[sectors[i].lines[j]].b;
            ctx.moveTo(vertices[a].x + xPos, vertices[a].y + yPos);
            ctx.lineTo(vertices[b].x + xPos, vertices[b].y + yPos);
        }
        ctx.stroke();
    }
}

function drawLines() {
    ctx.strokeStyle = "#FFFF00";
    if (startEditIndex_Line == -1) return;

    ctx.beginPath();
    for (let i = startEditIndex_Line; i < lines.length; i++) {
        ctx.moveTo(vertices[lines[i].a].x + xPos, vertices[lines[i].a].y + yPos);
        ctx.lineTo(vertices[lines[i].b].x + xPos, vertices[lines[i].b].y + yPos);
    }
    ctx.stroke();
}

function drawVertices() {
    ctx.fillStyle = "#FFFF00";

    if (startEditIndex_Vertex == -1) return;

    for (let i = startEditIndex_Vertex; i < vertices.length; i++) {
        if (i != startEditIndex_Vertex)
            ctx.fillRect(vertices[i].x + xPos - 4, vertices[i].y + yPos - 4, 8, 8);
    }
    
    ctx.fillStyle = "#FF0000";
    ctx.fillRect(vertices[startEditIndex_Vertex].x + xPos - 4, vertices[startEditIndex_Vertex].y + yPos - 4, 8, 8);
}

function drawGrid() {
    ctx.strokeStyle = "white";
    ctx.globalAlpha = 0.5;
    ctx.beginPath();
    for (let i = 0; i < canvas.width; i += gridSize) {
        ctx.moveTo((i + (xPos % gridSize)) * zoom, 0);
        ctx.lineTo((i + (xPos % gridSize)) * zoom, canvas.height);
    }
    for (let i = 0; i < canvas.width; i += gridSize) {
        ctx.moveTo(0, (i + (yPos % gridSize)) * zoom);
        ctx.lineTo(canvas.width, (i + (yPos % gridSize)) * zoom);
    }
    ctx.stroke();

    ctx.strokeStyle = "#FF0000";
    ctx.beginPath();
    ctx.moveTo(0, yPos * zoom);
    ctx.lineTo(canvas.width * zoom, yPos * zoom);
    ctx.stroke();

    ctx.strokeStyle = "#00FF00";
    ctx.beginPath();
    ctx.moveTo(xPos * zoom, 0);
    ctx.lineTo(xPos * zoom, canvas.height * zoom);
    ctx.stroke();

    ctx.globalAlpha = 1.0;
}

addEventListener("wheel", (event) => {
    if (event.deltaY < 0) {
        //zoom *= 1.1;
    } else {
        //zoom /= 1.1;
    }
});

canvas.addEventListener('mousedown', function (evt) {
    if (evt.button == 0) {
        if (startEditIndex_Vertex != -1) {
            lines.push(Line(vertices.length - 1, vertices.length));
            console.log("Line: " + lines[lines.length - 1]);
        }

        if (startEditIndex_Vertex == -1) {
            startEditIndex_Vertex = vertices.length;
            startEditIndex_Line = lines.length;
        }

        vertices.push(Vector(
            Math.round((evt.clientX - xPos) / gridSize) * gridSize,
            Math.round((evt.clientY - yPos) / gridSize) * gridSize
        ));

        console.log("Vertex: " + vertices[vertices.length - 1]);

        for (let i = startEditIndex_Vertex; i < vertices.length - 1; i++) {
            if (vertices[i].x == vertices[vertices.length - 1].x && vertices[i].y == vertices[vertices.length - 1].y) {
                if (vertices.length - startEditIndex_Vertex <= 3) {
                    vertices.splice(vertices.length - 2, 2);
                    lines.splice(lines.length - 1, 1);
                    startEditIndex_Vertex = -1;
                    startEditIndex_Line = -1;
                    break;
                }

                vertices.splice(vertices.length - 1, 1);
                //lines.splice(lines.length - 1, 1);

                var vertexIndices = [];
                for (let j = startEditIndex_Vertex; j < vertices.length; j++) {
                    vertexIndices.push(j);
                }

                var p1 = vertices[vertexIndices[0]];
                var p2 = vertices[vertexIndices[1]];
                var p3 = vertices[vertexIndices[2]];
                var val = (p2.y - p1.y) * (p3.x - p2.x) -
                    (p2.x - p1.x) * (p3.y - p2.y);

                if (val < 0) { // Clockwise?
                    vertexIndices.reverse();
                }

                /*
                var sectorCenter = Vector(0, 0);
                for (let j = 0; j < vertexIndices.length; j++) {
                    sectorCenter.x += vertices[vertexIndices[j]].x;
                    sectorCenter.y += vertices[vertexIndices[j]].y;
                }

                sectorCenter.x /= vertexIndices.length;
                sectorCenter.y /= vertexIndices.length;

                var angle1 = Math.atan2(vertices[vertexIndices[0]].y - sectorCenter.y, vertices[vertexIndices[0]].x - sectorCenter.x) * (180 / Math.PI);
                var angle2 = Math.atan2(vertices[vertexIndices[1]].y - sectorCenter.y, vertices[vertexIndices[1]].x - sectorCenter.x) * (180 / Math.PI);

                console.log(0 + ", " + (angle2 - angle1));
                */

                var lineIndices = [];
                for (let j = startEditIndex_Line; j < lines.length; j++) {
                    lineIndices.push(j);
                }

                sectors.push(Sector(vertexIndices, lineIndices, 10, 5, false));

                for (let j = 0; j < lineIndices.length; j++) {
                    lines[lineIndices[j]].sector = sectors.length - 1;
                }

                console.log("Sector: " + sectors[sectors.length - 1]);

                startEditIndex_Vertex = -1;
                startEditIndex_Line = -1;

                //vertices = [];
                //lines.clear();
                break;
            }
        }
    }

    if (evt.button == 2) {
        previousMousePos.x = evt.clientX;
        previousMousePos.y = evt.clientY;
        pan = true;

        if (hoveringIndex != -1) {
            selectedIndex = hoveringIndex;
            floorHeight.value = sectors[hoveringIndex].floor;
            ceilingHeight.value = sectors[hoveringIndex].ceiling;
        } else {
            selectedIndex = -1;
        }
    }
});

canvas.addEventListener('mouseup', function (evt) {
    if (evt.button == 2) {
        pan = false;
    }
});

var selectedIndex = -1;
var hoveringIndex = -1;

canvas.addEventListener("mousemove", (event) => {

    if (pan) {
        xPos -= (previousMousePos.x - event.clientX) / zoom;
        yPos -= (previousMousePos.y - event.clientY) / zoom;

        if (Math.abs((previousMousePos.x - event.clientX)) > 0 || Math.abs((previousMousePos.y - event.clientY)) > 0) {
            selectedIndex = -1;
        }

        previousMousePos.x = event.clientX;
        previousMousePos.y = event.clientY;
    }

    hoveringIndex = -1;
    for (let i = 0; i < sectors.length; i++) {
        var verts = [];
        for (let j = 0; j < sectors[i].vertices.length; j++) {
            verts.push(new SAT.Vector(vertices[sectors[i].vertices[j]].x, vertices[sectors[i].vertices[j]].y));
        }
        const poly = new SAT.Polygon(new SAT.Vector(0, 0), verts);

        if (SAT.pointInPolygon(new SAT.Vector(event.clientX - xPos, event.clientY - yPos), poly)) {
            hoveringIndex = i;
            break;
        }
    }
});

function changeFloor() {
    if (selectedIndex != -1) {
        sectors[selectedIndex].floor = floorHeight.value;
    }
}

function changeCeiling() {
    if (selectedIndex != -1) {
        sectors[selectedIndex].ceiling = ceilingHeight.value;
    }
}

setupCanvas();