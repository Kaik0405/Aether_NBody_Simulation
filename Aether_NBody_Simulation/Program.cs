using System.Numerics;
using Aether_NBody_Simulation;
using Raylib_cs;

const int windowWidth = 1820;
const int windowHeight = 980;

Raylib.InitWindow(windowWidth, windowHeight, "Aether N-Body Simulation");
Raylib.SetTargetFPS(60);

var physicsEngine = new PhysicsEngine();
var bodies = GalaxyBuilder.CreateChaoticThreeBodySystem();

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
	// Si la estela está vacía o tiene solo un punto, no hay nada que dibujar.
	if (body.TrailPoints.Count < 2)
	{
		return;
	}

	// Iteramos sobre los puntos de la estela, dibujando líneas entre puntos consecutivos.
	for (int i = 1; i < body.TrailPoints.Count; i++)
	{
		// Convertimos posiciones del mundo (centradas en screenCenter) a coordenadas de pantalla.
		Vector2 previousPoint = screenCenter + body.TrailPoints[i - 1];
		Vector2 currentPoint = screenCenter + body.TrailPoints[i];

		// Calculamos la opacidad (alpha) en función de la antigüedad del punto.
		float progress = i / (float)body.TrailPoints.Count;

		// El alpha define la transparencia.
		byte alpha = (byte)(40 + progress * 215f);

		Color trailColor = new Color(body.Color.R,body.Color.G,body.Color.B, alpha);

		// Dibujamos un segmento de línea blanca entre dos puntos consecutivos de la estela.
		Raylib.DrawLineV(previousPoint, currentPoint, trailColor);
	}
}
