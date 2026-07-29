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
const int windowWidth = 1820;
const int windowHeight = 980;

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
		DrawTrail(body, screenCenter);

		// Mapeo simple de coordenadas del mundo a pantalla.
		Vector2 screenPosition = screenCenter + body.Position;
		Raylib.DrawCircleV(screenPosition, body.Radius, body.Color);
	}
}

static void DrawTrail(Body body, Vector2 screenCenter)
{
	// Sanidad: necesitamos al menos 2 puntos para dibujar una línea.
	// Si la estela está vacía o tiene solo un punto, no hay nada que dibujar.
	if (body.TrailPoints.Count < 2)
	{
		return;
	}

	// Iteramos sobre los puntos de la estela, dibujando líneas entre puntos consecutivos.
	// Así formamos una secuencia de segmentos que representan la trayectoria.
	for (int i = 1; i < body.TrailPoints.Count; i++)
	{
		// Convertimos posiciones del mundo (centradas en screenCenter) a coordenadas de pantalla.
		// screenCenter es el origen visual (en píxeles).
		Vector2 previousPoint = screenCenter + body.TrailPoints[i - 1];
		Vector2 currentPoint = screenCenter + body.TrailPoints[i];

		// Calculamos la opacidad (alpha) en función de la antigüedad del punto.
		// Los puntos más antiguos (i cercano a 1) tienen alpha baja (más transparentes).
		// Los puntos más recientes (i cercano a Count) tienen alpha alta (más opacos).
		// Esto crea un efecto de "fade" visual donde la estela se desvanece en el pasado.
		float progress = i / (float)body.TrailPoints.Count;
		// Alpha = 40 (muy transparente) a 255 (opaco).
		// 40 es el mínimo para que los puntos antiguos no sean completamente invisibles.
		byte alpha = (byte)(40 + progress * 215f);

		// Creamos un color blanco con la opacidad calculada.
		// Los 3 primeros 255 son RGB (blanco puro).
		// El alpha define la transparencia.
		Color trailColor = new Color((byte)255, (byte)255, (byte)255, alpha);

		// Dibujamos un segmento de línea blanca entre dos puntos consecutivos de la estela.
		// Raylib.DrawLineV dibuja una línea vectorial (de Vector2 a Vector2) con un color.
		Raylib.DrawLineV(previousPoint, currentPoint, trailColor);
	}
}
