using System.Numerics;
using Aether_NBody_Simulation;
using Raylib_cs;

const int windowWidth = 1820;
const int windowHeight = 980;

Raylib.InitWindow(windowWidth, windowHeight, "Aether N-Body Simulation");
Raylib.SetTargetFPS(60);

var repository = new SimulationRepository();
var scenes = GalaxyBuilder.GetAvailableScenes();
var physicsEngine = new PhysicsEngine();
var camera = new Camera2D
{
    Offset = new Vector2(windowWidth * 0.5f, windowHeight * 0.5f),
    Target = Vector2.Zero,
    Rotation = 0f,
    Zoom = 1f
};

int selectedSceneIndex = 0;
var bodies = CreateBodiesForScene(scenes[selectedSceneIndex]);
var savedSimulations = repository.GetSimulations();
bool paused = false;
string statusMessage = "Selecciona una escena o guarda la actual.";
string saveName = "Escena actual";

try
{
    while (!Raylib.WindowShouldClose())
    {
        float dt = paused ? 0f : Raylib.GetFrameTime();
        if (!paused)
        {
            physicsEngine.Update(bodies, dt);
        }

        UpdateCamera(ref camera, windowWidth, windowHeight);
        HandleUiInput(
            scenes,
            ref selectedSceneIndex,
            ref bodies,
            ref paused,
            ref statusMessage,
            ref saveName,
            repository,
            ref savedSimulations,
            ref camera);

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(0, 0, 0, 255));

        DrawBodies(bodies, camera);
        DrawUiPanel(scenes, selectedSceneIndex, savedSimulations, paused, statusMessage, saveName);

        Raylib.DrawText("Scroll para zoom | Click derecho + arrastrar | R recentrar", 14, 14, 18, new Color(255, 255, 255, 255));
        Raylib.DrawText($"FPS: {Raylib.GetFPS()}", 14, 36, 18, new Color(255, 255, 0, 255));
        Raylib.EndDrawing();
    }
}
finally
{
    Raylib.CloseWindow();
}

static List<Body> CreateBodiesForScene(GalaxyBuilder.SceneDefinition scene)
{
    var loadedScene = scene.Factory();
    return loadedScene.Bodies.Select(body => body.CloneBody()).ToList();
}

static void SwitchScene(
    IReadOnlyList<GalaxyBuilder.SceneDefinition> scenes,
    int index,
    ref int selectedSceneIndex,
    ref List<Body> bodies,
    ref string statusMessage,
    ref Camera2D camera)
{
    if (index < 0 || index >= scenes.Count)
    {
        return;
    }

    selectedSceneIndex = index;
    bodies = CreateBodiesForScene(scenes[index]);
    statusMessage = $"Escena activa: {scenes[index].Name}";
    camera.Target = Vector2.Zero;
    camera.Zoom = 1f;
}

static void HandleUiInput(
    IReadOnlyList<GalaxyBuilder.SceneDefinition> scenes,
    ref int selectedSceneIndex,
    ref List<Body> bodies,
    ref bool paused,
    ref string statusMessage,
    ref string saveName,
    SimulationRepository repository,
    ref List<SimulationRecord> savedSimulations,
    ref Camera2D camera)
{
    if (Raylib.IsKeyPressed((KeyboardKey)32))
    {
        paused = !paused;
        statusMessage = paused ? "Simulación pausada" : "Simulación reanudada";
    }

    if (Raylib.IsKeyPressed(KeyboardKey.C))
    {
        int nextIndex = (selectedSceneIndex + 1) % scenes.Count;
        SwitchScene(scenes, nextIndex, ref selectedSceneIndex, ref bodies, ref statusMessage, ref camera);
    }

    if (Raylib.IsKeyPressed(KeyboardKey.S))
    {
        var snapshot = bodies.Select(body => body.CloneBody()).ToList();
        var id = repository.SaveSimulation(saveName, snapshot);
        savedSimulations = repository.GetSimulations();
        statusMessage = $"Guardada '{saveName}' con ID {id}";
    }

    if (Raylib.IsKeyPressed(KeyboardKey.L))
    {
        if (savedSimulations.Count > 0)
        {
            var latest = savedSimulations[0];
            var loaded = repository.LoadSimulation(latest.Id);
            bodies = loaded;
            statusMessage = $"Cargada '{latest.Name}'";
            camera.Target = Vector2.Zero;
            camera.Zoom = 1f;
        }
    }

    if (Raylib.IsKeyPressed(KeyboardKey.R))
    {
        camera.Target = Vector2.Zero;
        camera.Zoom = 1f;
        statusMessage = "Cámara reiniciada";
    }

    var mouse = Raylib.GetMousePosition();
    var scenePanel = new Rectangle(20, 70, 320, 360);
    var savePanel = new Rectangle(windowWidth - 340, 70, 320, 500);

    if (Raylib.IsMouseButtonPressed((MouseButton)0))
    {
        if (Raylib.CheckCollisionPointRec(mouse, scenePanel))
        {
            int index = (int)((mouse.Y - scenePanel.Y - 35) / 38f);
            if (index >= 0 && index < scenes.Count)
            {
                SwitchScene(scenes, index, ref selectedSceneIndex, ref bodies, ref statusMessage, ref camera);
            }
        }

        if (Raylib.CheckCollisionPointRec(mouse, savePanel))
        {
            int row = (int)((mouse.Y - savePanel.Y - 40) / 28f);
            if (row == 0)
            {
                saveName = $"{scenes[selectedSceneIndex].Name} - {DateTime.Now:HH:mm}";
                var snapshot = bodies.Select(body => body.CloneBody()).ToList();
                var id = repository.SaveSimulation(saveName, snapshot);
                savedSimulations = repository.GetSimulations();
                statusMessage = $"Guardada '{saveName}' con ID {id}";
            }
            else if (row > 0 && row - 1 < savedSimulations.Count)
            {
                var selected = savedSimulations[row - 1];
                var loaded = repository.LoadSimulation(selected.Id);
                bodies = loaded;
                statusMessage = $"Cargada '{selected.Name}'";
                camera.Target = Vector2.Zero;
                camera.Zoom = 1f;
            }
        }
    }
}

static void UpdateCamera(ref Camera2D camera, int windowWidth, int windowHeight)
{
    float wheelMove = Raylib.GetMouseWheelMove();
    if (wheelMove != 0f)
    {
        float zoomFactor = 1f + wheelMove * 0.1f;
        camera.Zoom = Math.Clamp(camera.Zoom * zoomFactor, 0.05f, 8f);
    }

    if (Raylib.IsMouseButtonDown((MouseButton)1))
    {
        Vector2 delta = Raylib.GetMouseDelta();
        camera.Target -= delta / camera.Zoom;
    }

    camera.Offset = new Vector2(windowWidth * 0.5f, windowHeight * 0.5f);
}

static void DrawBodies(List<Body> bodies, Camera2D camera)
{
    Raylib.BeginMode2D(camera);

    foreach (var body in bodies)
    {
        DrawTrail(body);
        Raylib.DrawCircleV(body.Position, body.Radius, body.Color);
    }

    Raylib.EndMode2D();
}

static void DrawTrail(Body body)
{
    if (body.TrailPoints.Count < 2) return;

    for (int i = 1; i < body.TrailPoints.Count; i++)
    {
        Vector2 prev = body.TrailPoints[i - 1];
        Vector2 cur = body.TrailPoints[i];
        float progress = i / (float)body.TrailPoints.Count;
        byte alpha = (byte)(30 + progress * 225f);
        Color trailColor = new Color(body.Color.R, body.Color.G, body.Color.B, alpha);
        Raylib.DrawLineV(prev, cur, trailColor);
    }
}

static void DrawUiPanel(
    IReadOnlyList<GalaxyBuilder.SceneDefinition> scenes,
    int selectedSceneIndex,
    List<SimulationRecord> savedSimulations,
    bool paused,
    string statusMessage,
    string saveName)
{
    var leftPanel = new Rectangle(20, 70, 320, 360);
    var rightPanel = new Rectangle(windowWidth - 340, 70, 320, 500);

    Raylib.DrawRectangleRec(leftPanel, new Color(10, 10, 20, 220));
    Raylib.DrawRectangleLinesEx(leftPanel, 2f, new Color(180, 180, 255, 255));
    Raylib.DrawText("Escenas", 38, 84, 24, new Color(255, 255, 255, 255));

    for (int i = 0; i < scenes.Count; i++)
    {
        var rect = new Rectangle(36, 120 + i * 38, 288, 30);
        bool hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), rect);
        Color fill = i == selectedSceneIndex ? new Color(80, 120, 255, 220) : new Color(25, 25, 35, 220);
        if (hovered)
        {
            fill = new Color(95, 140, 255, 240);
        }

        Raylib.DrawRectangleRec(rect, fill);
        Raylib.DrawRectangleLinesEx(rect, 1f, new Color(255, 255, 255, 160));
        Raylib.DrawText(scenes[i].Name, 46, 126 + i * 38, 18, new Color(255, 255, 255, 255));
        Raylib.DrawText(scenes[i].Description, 46, 144 + i * 38, 12, new Color(180, 200, 255, 255));
    }

    Raylib.DrawRectangleRec(rightPanel, new Color(10, 10, 20, 220));
    Raylib.DrawRectangleLinesEx(rightPanel, 2f, new Color(180, 180, 255, 255));
    Raylib.DrawText("Guardar / Cargar", 1520, 84, 24, new Color(255, 255, 255, 255));

    var saveButton = new Rectangle(windowWidth - 308, 120, 248, 38);
    Raylib.DrawRectangleRec(saveButton, new Color(40, 120, 70, 220));
    Raylib.DrawRectangleLinesEx(saveButton, 1f, new Color(255, 255, 255, 180));
    Raylib.DrawText("Guardar escena actual", (int)saveButton.X + 16, (int)saveButton.Y + 10, 18, new Color(255, 255, 255, 255));

    Raylib.DrawText("Guardados:", windowWidth - 308, 180, 20, new Color(255, 255, 255, 255));
    for (int i = 0; i < Math.Min(savedSimulations.Count, 8); i++)
    {
        var itemRect = new Rectangle(windowWidth - 308, 210 + i * 28, 248, 22);
        bool hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), itemRect);
        Color fill = hovered ? new Color(70, 90, 120, 240) : new Color(20, 20, 30, 220);
        Raylib.DrawRectangleRec(itemRect, fill);
        Raylib.DrawText(savedSimulations[i].Name, (int)itemRect.X + 8, (int)itemRect.Y + 4, 16, new Color(255, 255, 255, 255));
    }

    Raylib.DrawText($"Estado: {statusMessage}", 24, 460, 18, new Color(255, 255, 180, 255));
    Raylib.DrawText($"Nombre: {saveName}", 24, 485, 18, new Color(255, 255, 180, 255));
    Raylib.DrawText(paused ? "[PAUSADA]" : "[EN VIVO]", 24, 510, 18, new Color(255, 255, 255, 255));
    Raylib.DrawText("Teclas: C = siguiente escena | S = guardar | L = cargar última | Espacio = pausar", 24, 540, 18, new Color(180, 200, 255, 255));
}