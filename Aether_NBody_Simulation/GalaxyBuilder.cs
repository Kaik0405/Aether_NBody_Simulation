using System.Numerics;
using Raylib_cs;

namespace Aether_NBody_Simulation;
public static class GalaxyBuilder
{
    public static List<Body> CreateTestGalaxy()
    {
        // === SISTEMA DE PRUEBA: 1 SOL + 4 PLANETAS EN ÓRBITA ===
        //
        // Los parámetros (posición, velocidad, masa) fueron ajustados empíricamente
        // para que las órbitas sean visualmente claras y estables durante la simulación.
        //
        // Nota: Estas no son órbitas realistas (velocidades exageradas para visualización),
        // pero demuestran bien la mecánica gravitatoria.

        return new List<Body>
        {
            new Body(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 4500000f,
                radius: 50f,
                color: new Color(255, 255, 0, 255)), // Amarillo para el sol

            new Body(
                position: new Vector2(420f, 0f),
                velocity: new Vector2(0f, 95f),
                mass: 180f,
                radius: 10f,
                color: new Color(0, 0, 255, 255)), // Azul para el planeta principal

            new Body(
                position: new Vector2(0f, 360f),
                velocity: new Vector2(-100f, 0f),
                mass: 140f,
                radius: 8f,
                color: new Color(0, 255, 0, 255)), // Verde para un planeta secundario

            new Body(
                position: new Vector2(-560f, 0f),
                velocity: new Vector2(0f, -82f),
                mass: 110f,
                radius: 7f,
                color: new Color(255, 0, 255, 255)), // Magenta para otro planeta

            new Body(
                position: new Vector2(0f, -470f),
                velocity: new Vector2(90f, 0f),
                mass: 100f,
                radius: 6f,
                color: new Color(255, 165, 0, 255)) // Naranja para el cuarto planeta
        };
    }

    /// <summary>
    /// Crea una configuración de tres cuerpos pensada para exhibir un comportamiento caótico.
    /// </summary>
    /// 
    public static List<Body> CreateChaoticThreeBodySystem()
    {
        // Las masas ahora son iguales y el triángulo inicial es más compacto.
        // Con eso los cuerpos se mantienen más “pegados” y se influencia entre sí de forma más visible.
        //
        // La pequeña asimetría en las velocidades conserva un comportamiento sensible/caótico,
        // pero sin que el sistema se desarme tan rápido.
        return new List<Body>
        {
            new Body(
                position: new Vector2(-120f, -70f),
                velocity: new Vector2(21f, 41f),
                mass: 320000f,
                radius: 14f,
                color: new Color(255, 120, 120, 255)), // Cuerpo 1: rojo suave

            new Body(
                position: new Vector2(120f, -70f),
                velocity: new Vector2(-19f, 39f),
                mass: 320000f,
                radius: 14f,
                color: new Color(120, 255, 160, 255)), // Cuerpo 2: verde suave

            new Body(
                position: new Vector2(0f, 140f),
                velocity: new Vector2(-2f, -80f),
                mass: 320000f,
                radius: 14f,
                color: new Color(120, 180, 255, 255)) // Cuerpo 3: azul suave
        };
    }
}