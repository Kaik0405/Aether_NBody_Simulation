using System.Numerics;
using Aether_NBody_Simulation;
using Raylib_cs;

/// <summary>
/// Punto de entrada de la aplicación.
/// </summary>
/// <remarks>
/// Inicializa la ventana, crea un sistema de prueba y ejecuta el bucle principal
/// a 60 FPS con renderizado simple de los cuerpos.
/// </remarks>
const int windowWidth = 1280;
const int windowHeight = 720;

Raylib.InitWindow(windowWidth, windowHeight, "Aether N-Body Simulation");
Raylib.SetTargetFPS(60);

var physicsEngine = new PhysicsEngine();
var bodies = GalaxyBuilder.CreateTestGalaxy();

try
{
	while (!Raylib.WindowShouldClose())
	{
		// Delta time entregado por Raylib para mantener el paso de simulación estable.
		float dt = Raylib.GetFrameTime();
		physicsEngine.Update(bodies, dt);

		// Limpieza y dibujo de cada frame.
		Raylib.BeginDrawing();
		// Usar colores explícitos para compatibilidad con distintas versiones de Raylib-cs.
		Raylib.ClearBackground(new Color(0, 0, 0, 255));

		DrawBodies(bodies, windowWidth, windowHeight);

		Raylib.EndDrawing();
	}
}
finally
{
	// Cierre explícito para liberar recursos nativos de Raylib.
	Raylib.CloseWindow();
}

/// <summary>
/// Dibuja los cuerpos del sistema en pantalla.
/// </summary>
/// <param name="bodies">Cuerpos a representar.</param>
/// <param name="windowWidth">Ancho de la ventana.</param>
/// <param name="windowHeight">Alto de la ventana.</param>
/// 
static void DrawBodies(List<Body> bodies, int windowWidth, int windowHeight)
{
	// Centro de la ventana usado como origen visual de prueba.
	Vector2 screenCenter = new(windowWidth * 0.5f, windowHeight * 0.5f);

	for (int i = 0; i < bodies.Count; i++)
	{
		Body body = bodies[i];
		// Mapeo simple de coordenadas del mundo a pantalla.
		Vector2 screenPosition = screenCenter + body.Position;
		// Colores básicos para distinguir el sol del planeta.

		Raylib.DrawCircleV(screenPosition, body.Radius, body.Color);
	}
}
