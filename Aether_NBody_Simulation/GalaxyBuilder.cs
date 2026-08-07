using System.Numerics;
using Raylib_cs;

namespace Aether_NBody_Simulation;
public static class GalaxyBuilder
{
    public sealed record SceneDefinition(string Name, string Description, Func<SimulationScene> Factory);

    public static IReadOnlyList<SceneDefinition> GetAvailableScenes() => new List<SceneDefinition>
    {
        new("Sistema de prueba", "Un sistema solar simple con una estrella y varios planetas.", () => new SolarSystemScene("Sistema de prueba", "Un sistema solar simple con una estrella y varios planetas.", CreateTestGalaxy())),
        new("Tres cuerpos caótico", "Un sistema compacto con un agujero negro y dos cuerpos en órbita.", () => new SolarSystemScene("Tres cuerpos caótico", "Un sistema compacto con un agujero negro y dos cuerpos en órbita.", CreateChaoticThreeBodySystem())),
        new("Sistema binario", "Dos estrellas y un planeta que orbita alrededor del par.", () => new SolarSystemScene("Sistema binario", "Dos estrellas y un planeta que orbita alrededor del par.", CreateBinaryStarSystem())),
        new("Cinturón de asteroides", "Un sol central con miles de asteroides en órbita.", () => new GalaxyScene("Cinturón de asteroides", "Un sol central con miles de asteroides en órbita.", CreateAsteroidBeltSystem())),
        new("Anillo orbital", "Un sistema de anillo con satélites alrededor de una estrella central.", () => new GalaxyScene("Anillo orbital", "Un sistema de anillo con satélites alrededor de una estrella central.", CreateOrbitalRingSystem()))
    };

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
            new StarBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 4500000f,
                radius: 50f,
                color: new Color(255, 255, 0, 255)), // Amarillo para el sol

            new PlanetBody(
                position: new Vector2(420f, 0f),
                velocity: new Vector2(0f, 95f),
                mass: 180f,
                radius: 10f,
                color: new Color(0, 0, 255, 255)), // Azul para el planeta principal

            new PlanetBody(
                position: new Vector2(0f, 360f),
                velocity: new Vector2(-100f, 0f),
                mass: 140f,
                radius: 8f,
                color: new Color(0, 255, 0, 255)), // Verde para un planeta secundario

            new PlanetBody(
                position: new Vector2(-560f, 0f),
                velocity: new Vector2(0f, -82f),
                mass: 110f,
                radius: 7f,
                color: new Color(255, 0, 255, 255)), // Magenta para otro planeta

            new PlanetBody(
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
            new BlackHoleBody(
                position: new Vector2(-120f, -70f),
                velocity: new Vector2(21f, 41f),
                mass: 320000f,
                radius: 14f,
                color: new Color(255, 120, 120, 255)), // Cuerpo 1: rojo suave

            new PlanetBody(
                position: new Vector2(120f, -70f),
                velocity: new Vector2(-19f, 39f),
                mass: 320000f,
                radius: 14f,
                color: new Color(120, 255, 160, 255)), // Cuerpo 2: verde suave

            new PlanetBody(
                position: new Vector2(0f, 140f),
                velocity: new Vector2(-2f, -80f),
                mass: 320000f,
                radius: 14f,
                color: new Color(120, 180, 255, 255)) // Cuerpo 3: azul suave
        };
    }

    /// <summary>
    /// Crea dos estrellas orbitando entre sí con un planeta lejano.
    /// </summary>
    public static List<Body> CreateBinaryStarSystem()
    {
        return new List<Body>
        {
            new StarBody(
                position: new Vector2(-120f, 0f),
                velocity: new Vector2(0f, -55f),
                mass: 1800000f,
                radius: 24f,
                color: new Color(255, 210, 120, 255)),

            new StarBody(
                position: new Vector2(120f, 0f),
                velocity: new Vector2(0f, 55f),
                mass: 1800000f,
                radius: 24f,
                color: new Color(255, 160, 80, 255)),

            new PlanetBody(
                position: new Vector2(0f, 380f),
                velocity: new Vector2(-88f, 0f),
                mass: 160f,
                radius: 8f,
                color: new Color(120, 190, 255, 255))
        };
    }

    /// <summary>
    /// Genera un sistema visualmente rico con un sol central y múltiples asteroides.
    /// </summary>
    public static List<Body> CreateAsteroidBeltSystem()
    {
        var bodies = new List<Body>
        {
            new StarBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 4000000f,
                radius: 46f,
                color: new Color(255, 240, 140, 255))
        };

        // Cinturón con cuerpos pequeños a distintas distancias para dar variedad visual.
        for (int i = 0; i < 1000; i++)
        {
            float angle = i * MathF.Tau / 18f;
            float radius = 220f + (i % 4) * 34f;
            Vector2 position = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Vector2 tangent = new Vector2(-position.Y, position.X);
            tangent = Vector2.Normalize(tangent);

            float speed = 125f + (i % 5) * 8f;
            bodies.Add(new AsteroidBody(
                position: position,
                velocity: tangent * speed,
                mass: 18f + (i % 3) * 6f,
                radius: 3.5f + (i % 3),
                color: new Color((byte)(120 + (i * 7) % 100), (byte)(120 + (i * 13) % 100), (byte)(120 + (i * 17) % 100), (byte)255)));
        }

        return bodies;
    }

    /// <summary>
    /// Añade un sistema visual con un cuerpo central y un anillo de satélites en órbita.
    /// </summary>
    public static List<Body> CreateOrbitalRingSystem()
    {
        var bodies = new List<Body>
        {
            new StarBody(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 3000000f,
                radius: 34f,
                color: new Color(140, 220, 255, 255))
        };

        for (int i = 0; i < 24; i++)
        {
            float angle = i * MathF.Tau / 24f;
            Vector2 position = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 280f;
            Vector2 tangent = new Vector2(-position.Y, position.X);
            tangent = Vector2.Normalize(tangent);

            bodies.Add(new PlanetBody(
                position: position,
                velocity: tangent * (90f + (i % 4) * 8f),
                mass: 90f,
                radius: 5f + (i % 3),
                color: new Color((byte)(180 + (i * 7) % 60), (byte)(120 + (i * 13) % 70), (byte)(220 - (i * 5) % 40), (byte)255)));
        }

        return bodies;
    }
}