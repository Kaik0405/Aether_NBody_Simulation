using System.Numerics;
using Raylib_cs;

namespace Aether_NBody_Simulation;

/// <summary>
/// Fábrica de sistemas de prueba y futuras galaxias procedurales.
/// </summary>
public static class GalaxyBuilder
{
    /// <summary>
    /// Crea una galaxia de prueba mínima con un sol y un planeta.
    /// </summary>
    /// <returns>Una lista de cuerpos lista para simular y renderizar.</returns>
    /// <remarks>
    /// Este método existe como punto de partida para validar el renderizado,
    /// la física y la futura generación procedural.
    /// </remarks>
    public static List<Body> CreateTestGalaxy()
    {
        return new List<Body>
        {
            new Body(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 1000f,
                radius: 18f,
                color: new Color(255, 255, 0, 255)), // Amarillo para el sol
            new Body(
                position: new Vector2(220f, 0f),
                velocity: new Vector2(0f, 90f),
                mass: 1f,
                radius: 6f,
                color: new Color(0, 0, 255, 255)) // Azul para el planeta
        };
    }
}