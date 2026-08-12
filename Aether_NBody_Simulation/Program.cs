using System.Numerics;
using Aether_NBody_Simulation;
using Raylib_cs;

const int initialWindowWidth = 1820;
const int initialWindowHeight = 980;

// Enable window resizing so maximize/fullscreen work naturally.
Raylib.SetConfigFlags((ConfigFlags)4);
Raylib.InitWindow(initialWindowWidth, initialWindowHeight, "Aether N-Body Simulation");
Raylib.SetTargetFPS(60);

// Start maximized to use the available screen space immediately.
Raylib.MaximizeWindow();

// Servicios principales: almacenamiento, catálogo de escenas y motor físico.
var repository = new SimulationRepository();
var scenes = GalaxyBuilder.GetAvailableScenes();
var physicsEngine = new PhysicsEngine();

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
var savedSimulations = repository.GetSimulations();
int selectedSaveId = savedSimulations.FirstOrDefault()?.Id ?? -1;
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

        // El movimiento de cámara y la interacción con la interfaz se procesan por separado para mantener el flujo claro.
        UpdateCamera(ref camera, screenWidth, screenHeight);
        HandleUiInput(
            scenes,
            ref selectedSceneIndex,
            ref bodies,
            ref selectedSaveId,
            ref paused,
            ref editingSaveName,
            ref statusMessage,
            ref saveName,
            repository,
            ref savedSimulations,
            ref camera,
            screenWidth,
            screenHeight);

        // Se limpia y vuelve a dibujar el frame completo en cada iteración.
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(0, 0, 0, 255));

        DrawBodies(bodies, camera);
        DrawUiPanel(
            scenes,
            selectedSceneIndex,
            savedSimulations,
            selectedSaveId,
            editingSaveName,
            statusMessage,
            saveName,
            screenWidth,
            screenHeight);

        Raylib.EndDrawing();
    }
}
finally
{
    // Clean shutdown to release Raylib native resources.
    Raylib.CloseWindow();
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

    // Recenter the camera so the newly loaded scene is immediately visible.
    camera.Target = Vector2.Zero;
    camera.Zoom = 1f;
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
            editingSaveName = false;
        }
    }

    if (Raylib.IsKeyPressed((KeyboardKey)261) && selectedSaveId != -1)
    {
        DeleteSelectedSimulation(savedSimulations, selectedSaveId, repository, ref savedSimulations, ref selectedSaveId, ref statusMessage);
    }
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
        camera.Zoom = Math.Clamp(camera.Zoom * zoomFactor, 0.05f, 8f);
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

static void DrawBodies(List<Body> bodies, Camera2D camera)
{
    // Todo lo dibujado aquí se ve afectado por la cámara y su zoom.
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
    // If there are not enough points yet, skip the trail.
    if (body.TrailPoints.Count < 2)
    {
        return;
    }

    // Consecutive segments with increasing alpha create a smooth trail.
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
    int selectedSaveId,
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
    Raylib.DrawText($"Status: {ClipText(statusMessage, 70)}", (int)layout.BottomBar.X + 12, (int)layout.BottomBar.Y + 8, 14, new Color(255, 255, 180, 255));
    Raylib.DrawText($"Name: {ClipText(saveName, 26)}", (int)layout.BottomBar.X + 12, (int)layout.BottomBar.Y + 30, 12, new Color(255, 255, 255, 255));
    Raylib.DrawText($"FPS: {Raylib.GetFPS()}", (int)layout.BottomBar.X + (int)layout.BottomBar.Width - 70, (int)layout.BottomBar.Y + 8, 14, new Color(255, 255, 0, 255));
    Raylib.DrawText("F10 maximize | F11 fullscreen | C scene | Space pause", (int)layout.BottomBar.X + 12, (int)layout.BottomBar.Y + 46, 12, new Color(180, 200, 255, 255));
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
