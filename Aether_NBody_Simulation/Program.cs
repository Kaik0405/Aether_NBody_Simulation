using System.Numerics;
using System.Diagnostics;
using Aether_NBody_Simulation;
using Raylib_cs;

const int initialWindowWidth = 1820;
const int initialWindowHeight = 980;

var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToHashSet(StringComparer.OrdinalIgnoreCase);
if (cliArgs.Contains("--benchmark-quadtree"))
{
    RunPerformanceBenchmark();
    return;
}

bool externalUi = cliArgs.Contains("--external-ui");
bool hosted = cliArgs.Contains("--hosted");
if (!externalUi && !cliArgs.Contains("--embedded-ui"))
{
    LaunchExternalControlPanel();
    return;
}

// Enable window resizing so maximize/fullscreen work naturally.
Raylib.SetConfigFlags((ConfigFlags)4);
Raylib.InitWindow(initialWindowWidth, initialWindowHeight, "Aether N-Body Simulation");
Raylib.SetTargetFPS(60);

// Standalone mode maximizes; hosted mode is resized by the WPF parent window.
if (!hosted)
{
    Raylib.MaximizeWindow();
}

// Servicios principales: almacenamiento, catálogo de escenas y motor físico.
var repository = new SimulationRepository();
var scenes = GalaxyBuilder.GetAvailableScenes();
var physicsEngine = new PhysicsEngine
{
    SolverMode = PhysicsSolverMode.Quadtree,
    RecordTrails = false
};
using var controlPipe = externalUi ? new ControlPipeServer() : null;

// Cámara 2D usada para navegar por la simulación con zoom y desplazamiento.
var camera = new Camera2D
{
    Offset = new Vector2(initialWindowWidth * 0.5f, initialWindowHeight * 0.5f),
    Target = Vector2.Zero,
    Rotation = 0f,
    Zoom = 1f
};

// Estado de la aplicación: escena activa, cuerpos actuales, estados guardados y mensajes de UI.
int selectedSceneIndex = 0;
var bodies = CreateBodiesForScene(scenes[selectedSceneIndex]);
FrameBodies(ref camera, bodies, initialWindowWidth, initialWindowHeight);
var savedSimulations = repository.GetSimulations();
int selectedSaveId = savedSimulations.FirstOrDefault()?.Id ?? -1;
int selectedBodyIndex = -1;
bool paused = false;
bool editingSaveName = false;
string statusMessage = "Select a scene, edit a name, and save.";
string saveName = scenes[selectedSceneIndex].Name;

try
{
    while (!Raylib.WindowShouldClose())
    {
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();

        // Si la simulación está pausada, se evita actualizar la física pero la interfaz sigue respondiendo.
        float dt = paused ? 0f : Raylib.GetFrameTime();
        if (!paused)
        {
            physicsEngine.Update(bodies, dt);
        }

        if (externalUi)
        {
            ProcessExternalCommands(
                controlPipe!,
                scenes,
                ref selectedSceneIndex,
                ref bodies,
                ref paused,
                ref selectedBodyIndex,
                physicsEngine,
                ref camera,
                screenWidth,
                screenHeight);
        }

        // El movimiento de cámara y la interacción con la interfaz se procesan por separado para mantener el flujo claro.
        UpdateCamera(ref camera, screenWidth, screenHeight);
        if (!externalUi)
        {
            HandleUiInput(
            scenes,
            ref selectedSceneIndex,
            ref bodies,
            ref selectedSaveId,
            ref paused,
            ref editingSaveName,
            ref statusMessage,
            ref saveName,
            ref selectedBodyIndex,
            physicsEngine,
            repository,
            ref savedSimulations,
            ref camera,
            screenWidth,
            screenHeight);
        }

        // Se limpia y vuelve a dibujar el frame completo en cada iteración.
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(0, 0, 0, 255));

        DrawBodies(bodies, camera, screenWidth, screenHeight);
        if (!externalUi)
        {
            DrawUiPanel(
            scenes,
            selectedSceneIndex,
            bodies,
            savedSimulations,
            selectedSaveId,
            selectedBodyIndex,
            physicsEngine.SimulationSpeed,
            editingSaveName,
            statusMessage,
            saveName,
            screenWidth,
            screenHeight);
        }

        Raylib.EndDrawing();
    }
}
finally
{
    // Clean shutdown to release Raylib native resources.
    Raylib.CloseWindow();
}

static void ProcessExternalCommands(
    ControlPipeServer pipe,
    IReadOnlyList<GalaxyBuilder.SceneDefinition> scenes,
    ref int selectedSceneIndex,
    ref List<Body> bodies,
    ref bool paused,
    ref int selectedBodyIndex,
    PhysicsEngine physicsEngine,
    ref Camera2D camera,
    int screenWidth,
    int screenHeight)
{
    while (pipe.TryDequeue(out SimulationCommand? command) && command is not null)
    {
        switch (command.Name.ToLowerInvariant())
        {
            case "pause":
                paused = !paused;
                break;
            case "speed" when command.Arguments.Length > 0
                && float.TryParse(command.Arguments[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float speed):
                physicsEngine.SimulationSpeed = Math.Clamp(speed, 0.125f, 32f);
                break;
            case "scene" when command.Arguments.Length > 0:
                int sceneIndex = Enumerable.Range(0, scenes.Count).FirstOrDefault(index => scenes[index].Name.Contains(command.Arguments[0], StringComparison.OrdinalIgnoreCase), -1);
                if (sceneIndex >= 0)
                {
                    selectedSceneIndex = sceneIndex;
                    bodies = CreateBodiesForScene(scenes[sceneIndex]);
                    selectedBodyIndex = -1;
                    FrameBodies(ref camera, bodies, screenWidth, screenHeight);
                }
                break;
            case "spawn" when command.Arguments.Length >= 6:
                if (TryCreateExternalBody(command.Arguments, out Body? spawned))
                {
                    bodies.Add(spawned!);
                    selectedBodyIndex = bodies.Count - 1;
                }
                break;
            case "select" when command.Arguments.Length >= 2
                && float.TryParse(command.Arguments[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float selectX)
                && float.TryParse(command.Arguments[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float selectY):
                selectedBodyIndex = FindBodyAtPoint(bodies, new Vector2(selectX, selectY));
                WriteSelectionInfo(bodies, selectedBodyIndex);
                break;
            case "delete-selected" when selectedBodyIndex >= 0 && selectedBodyIndex < bodies.Count:
                bodies.RemoveAt(selectedBodyIndex);
                selectedBodyIndex = -1;
                break;
        }
    }
}

static void LaunchExternalControlPanel()
{
    string root = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName
        ?? Environment.CurrentDirectory;
    string panelProject = Path.Combine(root, "Aether_ControlPanel", "Aether_ControlPanel.csproj");

    Process.Start(new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{panelProject}\"",
        WorkingDirectory = root,
        UseShellExecute = false,
        CreateNoWindow = false
    });
}

static bool TryCreateExternalBody(string[] arguments, out Body? body)
{
    body = null;
    if (!float.TryParse(arguments[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float mass)
        || !float.TryParse(arguments[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float radius)
        || !float.TryParse(arguments[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)
        || !float.TryParse(arguments[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
    {
        return false;
    }

    Vector2 position = new(x, y);
    body = arguments[1].ToLowerInvariant() switch
    {
        "star" => new StarBody(position, Vector2.Zero, MathF.Max(1000f, mass), MathF.Max(5f, radius), new Color(255, 210, 100, 255), false),
        "blackhole" => new BlackHoleBody(position, Vector2.Zero, MathF.Max(100000f, mass), MathF.Max(15f, radius), new Color(8, 8, 14, 255), false),
        "pulsar" => new PulsarBody(position, Vector2.Zero, MathF.Max(100000f, mass), MathF.Max(8f, radius), new Color(150, 220, 255, 255), false),
        "quasar" => new QuasarBody(position, Vector2.Zero, MathF.Max(9000000f, mass), MathF.Max(30f, radius), new Color(255, 160, 70, 255), false),
        _ => new PlanetBody(position, Vector2.Zero, MathF.Max(1f, mass), MathF.Max(1f, radius), new Color(120, 190, 255, 255), false)
    };

    return true;
}

static void WriteSelectionInfo(IReadOnlyList<Body> bodies, int selectedBodyIndex)
{
    string text = selectedBodyIndex >= 0 && selectedBodyIndex < bodies.Count
        ? $"Selected: {bodies[selectedBodyIndex].TypeName}\nMass: {bodies[selectedBodyIndex].Mass:0.##}\nRadius: {bodies[selectedBodyIndex].Radius:0.##}\nVelocity: {bodies[selectedBodyIndex].Velocity.Length():0.##}\nPosition: {bodies[selectedBodyIndex].Position}"
        : "No body selected";
    File.WriteAllText("aether.selection", text);
}

// Se clona la escena para que la simulación no modifique la definición original.
static List<Body> CreateBodiesForScene(GalaxyBuilder.SceneDefinition scene)
{
    var loadedScene = scene.Factory();
    return loadedScene.Bodies.Select(body => body.CloneBody()).ToList();
}

// Cambia la escena activa por otra del catálogo y reinicia el estado visual.
static void SwitchScene(
    IReadOnlyList<GalaxyBuilder.SceneDefinition> scenes,
    int index,
    ref int selectedSceneIndex,
    ref List<Body> bodies,
    ref string statusMessage,
    ref string saveName,
    ref Camera2D camera)
{
    if (index < 0 || index >= scenes.Count)
    {
        return;
    }

    selectedSceneIndex = index;
    bodies = CreateBodiesForScene(scenes[index]);
    saveName = scenes[index].Name;
    statusMessage = $"Active scene: {scenes[index].Name}";

    // Las escenas pequeñas se centran; el universo de varias galaxias se encuadra completo.
    if (scenes[index].Name.Contains("Fase", StringComparison.OrdinalIgnoreCase))
    {
        FrameBodies(ref camera, bodies, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
    }
    else
    {
        camera.Target = Vector2.Zero;
        camera.Zoom = 1f;
    }
}

static void FrameBodies(ref Camera2D camera, IReadOnlyList<Body> bodies, int screenWidth, int screenHeight)
{
    if (bodies.Count == 0)
    {
        return;
    }

    float minX = bodies.Min(body => body.Position.X);
    float maxX = bodies.Max(body => body.Position.X);
    float minY = bodies.Min(body => body.Position.Y);
    float maxY = bodies.Max(body => body.Position.Y);
    Vector2 center = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    float worldWidth = MathF.Max(1f, maxX - minX);
    float worldHeight = MathF.Max(1f, maxY - minY);
    float zoomX = screenWidth * 0.72f / worldWidth;
    float zoomY = screenHeight * 0.72f / worldHeight;

    camera.Target = center;
    camera.Zoom = Math.Clamp(MathF.Min(zoomX, zoomY), 0.02f, 1f);
}

static void HandleUiInput(
    IReadOnlyList<GalaxyBuilder.SceneDefinition> scenes,
    ref int selectedSceneIndex,
    ref List<Body> bodies,
    ref int selectedSaveId,
    ref bool paused,
    ref bool editingSaveName,
    ref string statusMessage,
    ref string saveName,
    ref int selectedBodyIndex,
    PhysicsEngine physicsEngine,
    SimulationRepository repository,
    ref List<SimulationRecord> savedSimulations,
    ref Camera2D camera,
    int screenWidth,
    int screenHeight)
{
    // Se construye el layout de la UI a partir del tamaño actual de la ventana.
    UiLayout layout = BuildUiLayout(screenWidth, screenHeight);
    var mouse = Raylib.GetMousePosition();

    // F10 maximizes/restores and F11 toggles fullscreen.
    if (Raylib.IsKeyPressed((KeyboardKey)299))
    {
        if (Raylib.IsWindowMaximized())
        {
            Raylib.RestoreWindow();
        }
        else
        {
            Raylib.MaximizeWindow();
        }
    }

    if (Raylib.IsKeyPressed((KeyboardKey)300))
    {
        Raylib.ToggleFullscreen();
    }

    // Space: pause or resume the simulation.
    if (Raylib.IsKeyPressed((KeyboardKey)32))
    {
        paused = !paused;
        statusMessage = paused ? "Simulation paused" : "Simulation resumed";
    }

    // C: jump to the next available scene.
    if (Raylib.IsKeyPressed(KeyboardKey.C))
    {
        int nextIndex = (selectedSceneIndex + 1) % scenes.Count;
        SwitchScene(scenes, nextIndex, ref selectedSceneIndex, ref bodies, ref statusMessage, ref saveName, ref camera);
    }

    // R: reset the camera.
    if (Raylib.IsKeyPressed(KeyboardKey.R))
    {
        camera.Target = Vector2.Zero;
        camera.Zoom = 1f;
        statusMessage = "Camera reset";
    }

    // +/- cambia la velocidad de la simulación sin modificar la velocidad física base.
    if (Raylib.IsKeyPressed((KeyboardKey)61))
    {
        physicsEngine.SimulationSpeed = MathF.Min(32f, physicsEngine.SimulationSpeed * 2f);
        statusMessage = $"Simulation speed: {physicsEngine.SimulationSpeed:0.##}x";
    }
    if (Raylib.IsKeyPressed((KeyboardKey)45))
    {
        physicsEngine.SimulationSpeed = MathF.Max(0.125f, physicsEngine.SimulationSpeed * 0.5f);
        statusMessage = $"Simulation speed: {physicsEngine.SimulationSpeed:0.###}x";
    }

    // Si el campo de texto está activo, se capturan las teclas para editar el nombre del estado guardado.
    saveName = UpdateEditableText(saveName, editingSaveName, out editingSaveName);

    if (Raylib.IsMouseButtonPressed((MouseButton)0))
    {
        // Se usa un flag para evitar cerrar el modo edición si el clic fue sobre un botón o una lista.
        bool clickedUiElement = false;

        if (Raylib.CheckCollisionPointRec(mouse, layout.ScenePanel))
        {
            int index = (int)((mouse.Y - layout.ScenePanel.Y - 40f) / 44f);
            if (index >= 0 && index < scenes.Count)
            {
                SwitchScene(scenes, index, ref selectedSceneIndex, ref bodies, ref statusMessage, ref saveName, ref camera);
                clickedUiElement = true;
            }
        }

        if (Raylib.CheckCollisionPointRec(mouse, layout.SaveNameBox))
        {
            editingSaveName = true;
            statusMessage = "Editing save name";
            clickedUiElement = true;
        }
        else if (Raylib.CheckCollisionPointRec(mouse, layout.SaveButton))
        {
            SaveCurrentSimulation(repository, bodies, ref savedSimulations, ref selectedSaveId, ref statusMessage, ref saveName, scenes[selectedSceneIndex].Name);
            editingSaveName = false;
            clickedUiElement = true;
        }
        else if (Raylib.CheckCollisionPointRec(mouse, layout.LoadButton))
        {
            LoadSelectedSimulation(savedSimulations, selectedSaveId, repository, ref bodies, ref statusMessage, ref saveName, ref camera);
            clickedUiElement = true;
        }
        else if (Raylib.CheckCollisionPointRec(mouse, layout.RenameButton))
        {
            RenameSelectedSimulation(savedSimulations, selectedSaveId, saveName, repository, ref savedSimulations, ref statusMessage, ref saveName);
            clickedUiElement = true;
        }
        else if (Raylib.CheckCollisionPointRec(mouse, layout.DeleteButton))
        {
            DeleteSelectedSimulation(savedSimulations, selectedSaveId, repository, ref savedSimulations, ref selectedSaveId, ref statusMessage);
            clickedUiElement = true;
        }
        else if (Raylib.CheckCollisionPointRec(mouse, layout.SaveListPanel))
        {
            int row = (int)((mouse.Y - layout.SaveListPanel.Y - 8f) / 24f);
            if (row >= 0 && row < savedSimulations.Count)
            {
                var selected = savedSimulations[row];
                selectedSaveId = selected.Id;
                saveName = selected.Name;
                editingSaveName = false;
                statusMessage = $"Selected: {selected.Name}";
                clickedUiElement = true;
            }
        }

        if (!clickedUiElement)
        {
            // Un clic sobre un cuerpo lo selecciona para las acciones de edición de fase 3.
            Vector2 worldMouse = Raylib.GetScreenToWorld2D(mouse, camera);
            selectedBodyIndex = FindBodyAtPoint(bodies, worldMouse);
            clickedUiElement = selectedBodyIndex >= 0;
            if (clickedUiElement)
            {
                statusMessage = $"Selected body: {bodies[selectedBodyIndex].TypeName}";
            }
            else
            {
                editingSaveName = false;
            }
        }
    }

    if (Raylib.IsKeyPressed((KeyboardKey)261) && selectedSaveId != -1)
    {
        DeleteSelectedSimulation(savedSimulations, selectedSaveId, repository, ref savedSimulations, ref selectedSaveId, ref statusMessage);
    }

    if (Raylib.IsKeyPressed(KeyboardKey.N))
    {
        Vector2 worldMouse = Raylib.GetScreenToWorld2D(mouse, camera);
        bodies.Add(new PlanetBody(worldMouse, Vector2.Zero, 40f, 8f, new Color(120, 190, 255, 255), false));
        selectedBodyIndex = bodies.Count - 1;
        statusMessage = "Planet spawned";
    }

    if (selectedBodyIndex >= 0 && selectedBodyIndex < bodies.Count)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.X))
        {
            bodies.RemoveAt(selectedBodyIndex);
            selectedBodyIndex = -1;
            statusMessage = "Body removed";
        }
        else if (Raylib.IsKeyPressed((KeyboardKey)49))
        {
            bodies[selectedBodyIndex] = ReplaceBody(bodies[selectedBodyIndex], BodyKind.Planet);
        }
        else if (Raylib.IsKeyPressed((KeyboardKey)50))
        {
            bodies[selectedBodyIndex] = ReplaceBody(bodies[selectedBodyIndex], BodyKind.Star);
        }
        else if (Raylib.IsKeyPressed((KeyboardKey)51))
        {
            bodies[selectedBodyIndex] = ReplaceBody(bodies[selectedBodyIndex], BodyKind.BlackHole);
        }
        else if (Raylib.IsKeyPressed((KeyboardKey)52))
        {
            bodies[selectedBodyIndex] = ReplaceBody(bodies[selectedBodyIndex], BodyKind.Pulsar);
        }
        else if (Raylib.IsKeyPressed((KeyboardKey)53))
        {
            bodies[selectedBodyIndex] = ReplaceBody(bodies[selectedBodyIndex], BodyKind.Quasar);
        }
    }
}

static int FindBodyAtPoint(IReadOnlyList<Body> bodies, Vector2 point)
{
    int selected = -1;
    float closestDistance = float.MaxValue;
    for (int i = 0; i < bodies.Count; i++)
    {
        if (bodies[i].IsConsumed)
        {
            continue;
        }

        float distance = Vector2.DistanceSquared(point, bodies[i].Position);
        float hitRadius = MathF.Max(18f, bodies[i].Radius * 2f);
        if (distance <= hitRadius * hitRadius && distance < closestDistance)
        {
            selected = i;
            closestDistance = distance;
        }
    }

    return selected;
}

static Body ReplaceBody(Body source, BodyKind kind)
{
    Body replacement = kind switch
    {
        BodyKind.Planet => new PlanetBody(source.Position, source.Velocity, source.Mass, source.Radius, source.Color, false),
        BodyKind.Star => new StarBody(source.Position, source.Velocity, MathF.Max(1000f, source.Mass), MathF.Max(6f, source.Radius), source.Color, false),
        BodyKind.BlackHole => new BlackHoleBody(source.Position, source.Velocity, MathF.Max(100000f, source.Mass), MathF.Max(18f, source.Radius), new Color(8, 8, 14, 255), false),
        BodyKind.Pulsar => new PulsarBody(source.Position, source.Velocity, MathF.Max(100000f, source.Mass), MathF.Max(8f, source.Radius), new Color(150, 220, 255, 255), false),
        BodyKind.Quasar => new QuasarBody(source.Position, source.Velocity, MathF.Max(9000000f, source.Mass), MathF.Max(30f, source.Radius), new Color(255, 160, 70, 255), false),
        _ => source.CloneBody()
    };

    replacement.Acceleration = source.Acceleration;
    return replacement;
}

static void SaveCurrentSimulation(
    SimulationRepository repository,
    List<Body> bodies,
    ref List<SimulationRecord> savedSimulations,
    ref int selectedSaveId,
    ref string statusMessage,
    ref string saveName,
    string fallbackSceneName)
{
    // Si no hay nombre, se genera uno automático usando la escena y la hora actual.
    string finalName = string.IsNullOrWhiteSpace(saveName)
        ? $"{fallbackSceneName} - {DateTime.Now:HH:mm}"
        : saveName.Trim();

    // Se guarda una copia de los cuerpos para poder recuperar el estado más tarde.
    var snapshot = bodies.Select(body => body.CloneBody()).ToList();
    int id = repository.SaveSimulation(finalName, snapshot);
    savedSimulations = repository.GetSimulations();
    selectedSaveId = id;
    saveName = finalName;
    statusMessage = $"Saved '{finalName}'";
}

static void LoadSelectedSimulation(
    List<SimulationRecord> savedSimulations,
    int selectedSaveId,
    SimulationRepository repository,
    ref List<Body> bodies,
    ref string statusMessage,
    ref string saveName,
    ref Camera2D camera)
{
    var selected = savedSimulations.FirstOrDefault(simulation => simulation.Id == selectedSaveId);
    if (selected is null)
    {
        statusMessage = "Select a saved state before loading";
        return;
    }

    bodies = repository.LoadSimulation(selected.Id);
    saveName = selected.Name;
    statusMessage = $"Loaded '{selected.Name}'";
    camera.Target = Vector2.Zero;
    camera.Zoom = 1f;
}

static void RenameSelectedSimulation(
    List<SimulationRecord> savedSimulations,
    int selectedSaveId,
    string newName,
    SimulationRepository repository,
    ref List<SimulationRecord> refreshedSimulations,
    ref string statusMessage,
    ref string saveName)
{
    var selected = savedSimulations.FirstOrDefault(simulation => simulation.Id == selectedSaveId);
    if (selected is null)
    {
        statusMessage = "Select a saved state before renaming";
        return;
    }

    // El nombre nuevo se valida y se guarda si no está vacío.
    string finalName = string.IsNullOrWhiteSpace(newName) ? selected.Name : newName.Trim();
    bool renamed = repository.RenameSimulation(selected.Id, finalName);
    if (!renamed)
    {
        statusMessage = "Rename failed";
        return;
    }

    refreshedSimulations = repository.GetSimulations();
    saveName = finalName;
    statusMessage = $"Renamed to '{finalName}'";
}

static void DeleteSelectedSimulation(
    List<SimulationRecord> savedSimulations,
    int selectedSaveId,
    SimulationRepository repository,
    ref List<SimulationRecord> refreshedSimulations,
    ref int selectedSaveIdState,
    ref string statusMessage)
{
    var selected = savedSimulations.FirstOrDefault(simulation => simulation.Id == selectedSaveId);
    if (selected is null)
    {
        statusMessage = "Select a saved state before deleting";
        return;
    }

    bool deleted = repository.DeleteSimulation(selected.Id);
    if (!deleted)
    {
        statusMessage = "Delete failed";
        return;
    }

    refreshedSimulations = repository.GetSimulations();
    selectedSaveIdState = refreshedSimulations.FirstOrDefault()?.Id ?? -1;
    statusMessage = $"Deleted '{selected.Name}'";
}

static string UpdateEditableText(string currentText, bool editing, out bool keepEditing)
{
    keepEditing = editing;

    if (!editing)
    {
        return currentText;
    }

    // Se van acumulando las pulsaciones de teclado mientras el usuario está editando el nombre.
    int code;
    while ((code = Raylib.GetCharPressed()) > 0)
    {
        if (code >= 32 && code != 127 && currentText.Length < 48)
        {
            currentText += (char)code;
        }
    }

    // Retroceso borra el último carácter y Enter/Esc terminan el modo edición.
    if (Raylib.IsKeyPressed((KeyboardKey)259) && currentText.Length > 0)
    {
        currentText = currentText[..^1];
    }

    if (Raylib.IsKeyPressed((KeyboardKey)257) || Raylib.IsKeyPressed((KeyboardKey)256))
    {
        keepEditing = false;
    }

    return currentText;
}

static void UpdateCamera(ref Camera2D camera, int screenWidth, int screenHeight)
{
    // La rueda del ratón cambia el zoom de la vista.
    float wheelMove = Raylib.GetMouseWheelMove();
    if (wheelMove != 0f)
    {
        float zoomFactor = 1f + wheelMove * 0.1f;
        camera.Zoom = Math.Clamp(camera.Zoom * zoomFactor, 0.01f, 8f);
    }

    // Mantener el botón derecho pulsado permite desplazar la vista por el espacio.
    if (Raylib.IsMouseButtonDown((MouseButton)1))
    {
        Vector2 delta = Raylib.GetMouseDelta();
        camera.Target -= delta / camera.Zoom;
    }

    // Keep the camera centered on the current window.
    camera.Offset = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);
}

static void DrawBodies(List<Body> bodies, Camera2D camera, int screenWidth, int screenHeight)
{
    // Todo lo dibujado aquí se ve afectado por la cámara y su zoom.
    float halfWidth = screenWidth / (2f * camera.Zoom);
    float halfHeight = screenHeight / (2f * camera.Zoom);
    float left = camera.Target.X - halfWidth;
    float right = camera.Target.X + halfWidth;
    float top = camera.Target.Y - halfHeight;
    float bottom = camera.Target.Y + halfHeight;

    Raylib.BeginMode2D(camera);

    foreach (var body in bodies)
    {
        // El culling evita llamadas de dibujo para cuerpos fuera de la vista actual.
        if (!IsVisibleInCamera(body, left, right, top, bottom))
        {
            continue;
        }

        DrawBodyVisual(body);
    }

    Raylib.EndMode2D();
}

static bool IsVisibleInCamera(Body body, float left, float right, float top, float bottom)
{
    float padding = body.Radius;
    if (body is QuasarBody quasar)
    {
        padding = MathF.Max(padding, quasar.JetLength + quasar.JetWidth);
    }
    else if (body is PulsarBody pulsar)
    {
        padding = MathF.Max(padding, pulsar.JetLength + pulsar.Radius);
    }
    else if (body.IsBeingAbsorbed)
    {
        padding = MathF.Max(padding, body.OriginalRadius * 12f);
    }

    return body.Position.X + padding >= left
        && body.Position.X - padding <= right
        && body.Position.Y + padding >= top
        && body.Position.Y - padding <= bottom;
}

static void DrawBodyVisual(Body body)
{
    if (body.IsConsumed)
    {
        return;
    }

    if (body is QuasarBody quasar)
    {
        DrawQuasar(quasar);
        return;
    }

    if (body is BlackHoleBody blackHole)
    {
        DrawBlackHole(blackHole);
        return;
    }

    if (body is PulsarBody pulsar)
    {
        DrawPulsar(pulsar);
        return;
    }

    if (body.IsBeingAbsorbed)
    {
        DrawSpaghettifiedBody(body);
    }
    else
    {
        Raylib.DrawCircleV(body.Position, body.Radius, body.Color);
    }

    if (body.IsPixelating)
    {
        DrawPixelation(body);
    }
}

static void DrawBlackHole(BlackHoleBody blackHole)
{
    // El disco y el anillo luminoso crean una silueta 2D inspirada en Gargantua.
    Raylib.DrawCircleV(blackHole.Position, blackHole.Radius * 1.8f, new Color(255, 110, 35, 35));
    Raylib.DrawCircleV(blackHole.Position, blackHole.Radius * 1.35f, new Color(255, 190, 70, 80));
    Raylib.DrawCircleV(blackHole.Position, blackHole.Radius, new Color(4, 4, 10, 255));
    Raylib.DrawCircleLines((int)blackHole.Position.X, (int)blackHole.Position.Y, blackHole.Radius * 1.35f, new Color(255, 210, 120, 180));
}

static void DrawQuasar(QuasarBody quasar)
{
    // Un cuásar se distingue por su disco de acreción, no por los triángulos de un púlsar.
    Raylib.DrawEllipseV(quasar.Position, quasar.Radius * 3.2f, quasar.Radius * 0.9f, new Color(255, 95, 25, 45));
    Raylib.DrawEllipseLinesV(quasar.Position, quasar.Radius * 2.5f, quasar.Radius * 0.65f, new Color(255, 180, 60, 190));
    Raylib.DrawEllipseLinesV(quasar.Position, quasar.Radius * 1.8f, quasar.Radius * 0.45f, new Color(255, 235, 150, 230));
    DrawBlackHole(quasar);

    // Los jets del cuásar son haces estrechos y casi rectos, con brillo decreciente.
    Vector2 axis = Vector2.UnitY;
    DrawQuasarJet(quasar.Position - axis * quasar.Radius, axis, quasar.JetLength, quasar.JetWidth);
    DrawQuasarJet(quasar.Position + axis * quasar.Radius, axis * -1f, quasar.JetLength, quasar.JetWidth);
}

static void DrawQuasarJet(Vector2 origin, Vector2 direction, float length, float width)
{
    const int segments = 5;
    for (int i = 0; i < segments; i++)
    {
        float start = i / (float)segments;
        float end = (i + 1) / (float)segments;
        Vector2 segmentStart = origin + direction * (length * start);
        Vector2 segmentEnd = origin + direction * (length * end);
        float segmentWidth = MathF.Max(1f, width * (1f - start * 0.8f));
        byte alpha = (byte)(190f * (1f - start * 0.55f));
        Raylib.DrawLineEx(segmentStart, segmentEnd, segmentWidth, new Color((byte)120, (byte)205, (byte)255, alpha));
    }
}

static void DrawPulsar(PulsarBody pulsar)
{
    Raylib.DrawCircleV(pulsar.Position, pulsar.Radius * 1.35f, new Color(100, 190, 255, 45));
    Raylib.DrawCircleV(pulsar.Position, pulsar.Radius, pulsar.Color);

    Vector2 axis = new(MathF.Cos(pulsar.VisualRotation), MathF.Sin(pulsar.VisualRotation));
    Vector2 side = new(-axis.Y, axis.X);
    DrawPulsarJet(pulsar.Position + axis * pulsar.Radius, axis, pulsar.JetLength, pulsar.Radius * 0.7f, new Color(120, 220, 255, 180));
    DrawPulsarJet(pulsar.Position - axis * pulsar.Radius, axis * -1f, pulsar.JetLength, pulsar.Radius * 0.7f, new Color(120, 220, 255, 180));
    Raylib.DrawLineEx(pulsar.Position - side * pulsar.Radius, pulsar.Position + side * pulsar.Radius, 2f, new Color(255, 255, 255, 255));
}

static void DrawPulsarJet(Vector2 origin, Vector2 direction, float length, float width, Color color)
{
    Vector2 end = origin + direction * length;
    Vector2 side = new Vector2(-direction.Y, direction.X) * width;
    Raylib.DrawTriangle(origin, end + side, end - side, color);
    Raylib.DrawLineEx(origin, end, MathF.Max(1f, width * 0.18f), new Color(220, 245, 255, 220));
}

static void DrawSpaghettifiedBody(Body body)
{
    Vector2 direction = body.AbsorptionDirection.LengthSquared() > 0.0001f
        ? Vector2.Normalize(body.AbsorptionDirection)
        : Vector2.UnitX;
    Vector2 tailStart = body.Position - direction * body.Radius;
    float tailLength = body.OriginalRadius * (1.5f + body.AbsorptionProgress * 9f);
    Vector2 tailEnd = tailStart - direction * tailLength;
    float tailWidth = MathF.Max(1f, body.Radius * (1f - body.AbsorptionProgress * 0.65f));

    Raylib.DrawLineEx(tailStart, tailEnd, tailWidth, new Color(body.Color.R, body.Color.G, body.Color.B, (byte)100));
    Raylib.DrawLineEx(tailStart, tailEnd, MathF.Max(1f, tailWidth * 0.35f), body.Color);
    Raylib.DrawCircleV(body.Position, body.Radius, body.Color);
}

static void DrawPixelation(Body body)
{
    // Los bloques crecen y se separan para sugerir que el cuerpo cruza el horizonte.
    int count = 3 + (int)(body.PixelationProgress * 8f);
    for (int i = 0; i < count; i++)
    {
        float angle = i * 2.39996f + body.VisualRotation;
        float distance = body.Radius * (1.5f + i * 0.55f);
        Vector2 position = body.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
        float size = MathF.Max(1f, body.Radius * 0.35f);
        Raylib.DrawRectangleV(position, new Vector2(size, size), new Color(body.Color.R, body.Color.G, body.Color.B, (byte)255));
    }
}

static void DrawUiPanel(
    IReadOnlyList<GalaxyBuilder.SceneDefinition> scenes,
    int selectedSceneIndex,
    List<Body> bodies,
    List<SimulationRecord> savedSimulations,
    int selectedSaveId,
    int selectedBodyIndex,
    float simulationSpeed,
    bool editingSaveName,
    string statusMessage,
    string saveName,
    int screenWidth,
    int screenHeight)
{
    UiLayout layout = BuildUiLayout(screenWidth, screenHeight);

    Raylib.DrawRectangleRec(layout.ScenePanel, new Color(10, 10, 20, 220));
    Raylib.DrawRectangleLinesEx(layout.ScenePanel, 2f, new Color(180, 180, 255, 255));
    Raylib.DrawRectangleRec(layout.SavePanel, new Color(10, 10, 20, 220));
    Raylib.DrawRectangleLinesEx(layout.SavePanel, 2f, new Color(180, 180, 255, 255));

    Raylib.DrawText("Scenes", (int)layout.ScenePanel.X + 16, (int)layout.ScenePanel.Y + 12, 20, new Color(255, 255, 255, 255));
    Raylib.DrawText("Saved states", (int)layout.SavePanel.X + 16, (int)layout.SavePanel.Y + 12, 20, new Color(255, 255, 255, 255));

    const int sceneRowHeight = 44;
    for (int i = 0; i < scenes.Count; i++)
    {
        var row = new Rectangle(layout.ScenePanel.X + 10, layout.ScenePanel.Y + 38 + i * sceneRowHeight, layout.ScenePanel.Width - 20, 36);
        bool hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), row);
        Color fill = i == selectedSceneIndex ? new Color(70, 110, 255, 220) : new Color(25, 25, 35, 220);
        if (hovered)
        {
            fill = new Color(95, 140, 255, 240);
        }

        Raylib.DrawRectangleRec(row, fill);
        Raylib.DrawRectangleLinesEx(row, 1f, new Color(255, 255, 255, 120));
        Raylib.DrawText(ClipText(scenes[i].Name, 20), (int)row.X + 8, (int)row.Y + 8, 16, new Color(255, 255, 255, 255));
        Raylib.DrawText(ClipText(scenes[i].Description, 30), (int)row.X + 8, (int)row.Y + 22, 10, new Color(190, 205, 255, 255));
    }

    var nameBox = new Rectangle(layout.SavePanel.X + 12, layout.SavePanel.Y + 42, layout.SavePanel.Width - 24, 28);
    Raylib.DrawText("Name", (int)nameBox.X, (int)nameBox.Y - 12, 11, new Color(200, 200, 220, 255));
    Raylib.DrawRectangleRec(nameBox, editingSaveName ? new Color(50, 50, 70, 240) : new Color(30, 30, 42, 220));
    Raylib.DrawRectangleLinesEx(nameBox, 1f, new Color(255, 255, 255, 130));
    Raylib.DrawText(ClipText(saveName, 24), (int)nameBox.X + 8, (int)nameBox.Y + 6, 14, new Color(255, 255, 255, 255));

    var saveButton = new Rectangle(layout.SavePanel.X + 12, layout.SavePanel.Y + 80, (layout.SavePanel.Width - 28) / 2f, 28);
    var loadButton = new Rectangle(layout.SavePanel.X + 14 + (layout.SavePanel.Width - 28) / 2f, layout.SavePanel.Y + 80, (layout.SavePanel.Width - 28) / 2f, 28);
    var renameButton = new Rectangle(layout.SavePanel.X + 12, layout.SavePanel.Y + 116, (layout.SavePanel.Width - 28) / 2f, 28);
    var deleteButton = new Rectangle(layout.SavePanel.X + 14 + (layout.SavePanel.Width - 28) / 2f, layout.SavePanel.Y + 116, (layout.SavePanel.Width - 28) / 2f, 28);

    DrawButton(saveButton, "Save", new Color(40, 120, 70, 220), true);
    DrawButton(loadButton, "Load", new Color(55, 75, 120, 220), selectedSaveId != -1);
    DrawButton(renameButton, "Rename", new Color(110, 85, 40, 220), selectedSaveId != -1);
    DrawButton(deleteButton, "Delete", new Color(150, 55, 55, 220), selectedSaveId != -1);

    var listHeaderY = layout.SavePanel.Y + 156;
    Raylib.DrawText("Recent", (int)layout.SavePanel.X + 12, (int)listHeaderY - 14, 11, new Color(200, 200, 220, 255));

    var saveListPanel = new Rectangle(layout.SavePanel.X + 10, listHeaderY, layout.SavePanel.Width - 20, layout.SavePanel.Height - 174);
    const int saveRowHeight = 24;
    int visibleRows = Math.Min(savedSimulations.Count, (int)(saveListPanel.Height / saveRowHeight));
    for (int i = 0; i < visibleRows; i++)
    {
        var record = savedSimulations[i];
        var row = new Rectangle(saveListPanel.X, saveListPanel.Y + i * saveRowHeight, saveListPanel.Width, 20);
        bool hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), row);
        bool selected = record.Id == selectedSaveId;
        Color fill = selected ? new Color(75, 105, 150, 240) : new Color(20, 20, 30, 220);

        if (hovered)
        {
            fill = new Color(90, 120, 170, 245);
        }

        Raylib.DrawRectangleRec(row, fill);
        Raylib.DrawRectangleLinesEx(row, 1f, new Color(255, 255, 255, 90));
        Raylib.DrawText(ClipText(record.Name, 24), (int)row.X + 8, (int)row.Y + 3, 13, new Color(255, 255, 255, 255));
    }

    Raylib.DrawRectangleRec(layout.BottomBar, new Color(10, 10, 20, 220));
    Raylib.DrawRectangleLinesEx(layout.BottomBar, 2f, new Color(180, 180, 255, 255));
    string selectedInfo = selectedBodyIndex >= 0 && selectedBodyIndex < bodies.Count
        ? $"Selected: {bodies[selectedBodyIndex].TypeName} | M {bodies[selectedBodyIndex].Mass:0.##} | R {bodies[selectedBodyIndex].Radius:0.##} | V {bodies[selectedBodyIndex].Velocity.Length():0.##}"
        : "Selected: none";
    Raylib.DrawText($"Status: {ClipText(statusMessage, 70)}", (int)layout.BottomBar.X + 12, (int)layout.BottomBar.Y + 8, 14, new Color(255, 255, 180, 255));
    Raylib.DrawText($"Name: {ClipText(saveName, 26)}", (int)layout.BottomBar.X + 12, (int)layout.BottomBar.Y + 30, 12, new Color(255, 255, 255, 255));
    Raylib.DrawText(ClipText(selectedInfo, 78), (int)layout.BottomBar.X + 250, (int)layout.BottomBar.Y + 30, 12, new Color(180, 255, 200, 255));
    Raylib.DrawText($"Speed: {simulationSpeed:0.###}x", (int)layout.BottomBar.X + (int)layout.BottomBar.Width - 150, (int)layout.BottomBar.Y + 8, 12, new Color(255, 255, 0, 255));
    Raylib.DrawText($"FPS: {Raylib.GetFPS()}", (int)layout.BottomBar.X + (int)layout.BottomBar.Width - 70, (int)layout.BottomBar.Y + 8, 14, new Color(255, 255, 0, 255));
    float ms = Raylib.GetFrameTime() * 1000f;
    Raylib.DrawText($"Frame time: {ms:F2} ms", (int)layout.BottomBar.X + (int)layout.BottomBar.Width - 135, (int)layout.BottomBar.Y + 30, 12, new Color(255, 255, 0, 255));
    Raylib.DrawText("+/- speed | N spawn planet | X delete | 1-5 replace selected", (int)layout.BottomBar.X + 12, (int)layout.BottomBar.Y + 46, 12, new Color(180, 200, 255, 255));
}

static void DrawButton(Rectangle rect, string label, Color fill, bool enabled)
{
    Color finalFill = enabled ? fill : new Color(55, 55, 60, 180);
    Raylib.DrawRectangleRec(rect, finalFill);
    Raylib.DrawRectangleLinesEx(rect, 1f, new Color(255, 255, 255, 130));
    Raylib.DrawText(label, (int)rect.X + 10, (int)rect.Y + 7, 15, new Color(255, 255, 255, 255));
}

static UiLayout BuildUiLayout(int screenWidth, int screenHeight)
{
    // El layout se adapta al tamaño de la ventana para mantener la UI compacta y legible.
    int sideWidth = Math.Clamp(screenWidth / 6, 250, 320);
    int topY = 18;
    int bottomBarHeight = 64;
    int panelHeight = Math.Clamp(screenHeight - topY - bottomBarHeight - 20, 300, 460);

    var scenePanel = new Rectangle(16, topY, sideWidth, panelHeight);
    var savePanel = new Rectangle(screenWidth - sideWidth - 16, topY, sideWidth, panelHeight);
    var bottomBar = new Rectangle(16, screenHeight - bottomBarHeight - 10, screenWidth - 32, bottomBarHeight);

    return new UiLayout(
        scenePanel,
        savePanel,
        new Rectangle(savePanel.X + 12, savePanel.Y + 42, savePanel.Width - 24, 28),
        new Rectangle(savePanel.X + 12, savePanel.Y + 80, (savePanel.Width - 28) / 2f, 28),
        new Rectangle(savePanel.X + 14 + (savePanel.Width - 28) / 2f, savePanel.Y + 80, (savePanel.Width - 28) / 2f, 28),
        new Rectangle(savePanel.X + 12, savePanel.Y + 116, (savePanel.Width - 28) / 2f, 28),
        new Rectangle(savePanel.X + 14 + (savePanel.Width - 28) / 2f, savePanel.Y + 116, (savePanel.Width - 28) / 2f, 28),
        new Rectangle(savePanel.X + 10, savePanel.Y + 156, savePanel.Width - 20, savePanel.Height - 174),
        bottomBar);
}

static string ClipText(string text, int maxLength)
{
    if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
    {
        return text;
    }

    if (maxLength <= 1)
    {
        return "…";
    }

    return text[..(maxLength - 1)] + "…";
}

static void RunPerformanceBenchmark()
{
    // La comparación usa un número suficiente de cuerpos para que la diferencia entre O(n²) y Barnes-Hut sea visible.
    const float dt = 1f / 60f;
    const int compareBodies = 3000;
    const int stressBodies = 12000;
    const int compareSteps = 3;
    const int stressSteps = 6;

    Console.WriteLine("=== N-Body Benchmark (Naive vs Quadtree) ===");
    Console.WriteLine($"Comparison bodies: {compareBodies} | Stress bodies: {stressBodies}");
    Console.WriteLine();

    List<Body> comparisonTemplate = GalaxyBuilder.CreateQuadtreeStressSystem(compareBodies);
    MeasureScenario("Old method (Naive)", comparisonTemplate, PhysicsSolverMode.Naive, compareSteps, dt);
    MeasureScenario("New method (Quadtree)", comparisonTemplate, PhysicsSolverMode.Quadtree, compareSteps, dt);

    Console.WriteLine();
    List<Body> stressTemplate = GalaxyBuilder.CreateQuadtreeStressSystem(stressBodies);
    MeasureScenario("Quadtree stress test", stressTemplate, PhysicsSolverMode.Quadtree, stressSteps, dt);
}

static void MeasureScenario(
    string title,
    List<Body> templateBodies,
    PhysicsSolverMode solverMode,
    int steps,
    float dt)
{
    // Se clona el escenario base para que cada medición parta exactamente del mismo estado inicial.
    var bodies = templateBodies.Select(b => b.CloneBody()).ToList();
    var engine = new PhysicsEngine
    {
        SolverMode = solverMode,
        RecordTrails = false,
        Theta = 0.6f
    };

    // Warm-up corto para estabilizar JIT y evitar sesgo en la primera medida.
    engine.Update(bodies, dt);

    Stopwatch stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < steps; i++)
    {
        engine.Update(bodies, dt);
    }

    stopwatch.Stop();
    double totalMs = stopwatch.Elapsed.TotalMilliseconds;
    double msPerStep = totalMs / Math.Max(1, steps);

    Console.WriteLine($"[{title}]");
    Console.WriteLine($"Solver: {solverMode} | Bodies: {bodies.Count} | Steps: {steps}");
    Console.WriteLine($"Total: {totalMs:F2} ms | Avg/step: {msPerStep:F2} ms");
}

readonly record struct UiLayout(
    Rectangle ScenePanel,
    Rectangle SavePanel,
    Rectangle SaveNameBox,
    Rectangle SaveButton,
    Rectangle LoadButton,
    Rectangle RenameButton,
    Rectangle DeleteButton,
    Rectangle SaveListPanel,
    Rectangle BottomBar);
