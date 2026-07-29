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
        // === SISTEMA DE PRUEBA: 1 SOL + 4 PLANETAS EN ÓRBITA ===
        //
        // Los parámetros (posición, velocidad, masa) fueron ajustados empíricamente
        // para que las órbitas sean visualmente claras y estables durante la simulación.
        //
        // Nota: Estas no son órbitas realistas (velocidades exageradas para visualización),
        // pero demuestran bien la mecánica gravitatoria.

        return new List<Body>
        {
            // ===== CUERPO 0: SOL (central masivo) =====
            // Posición: origen (0, 0).
            // Velocidad: cero (el sol permanece en el centro).
            // Masa: 4.5M (masiva, domina el sistema).
            // Radio: 50px (visualmente grande para representar el sol).
            // Color: amarillo (Color estándar para soles).
            new Body(
                position: Vector2.Zero,
                velocity: Vector2.Zero,
                mass: 4500000f,
                radius: 50f,
                color: new Color(255, 255, 0, 255)), // Amarillo para el sol

            // ===== CUERPO 1: PLANETA AZUL =====
            // Posición: 420px a la derecha (eje X+).
            // Velocidad: 95px/s hacia arriba (eje Y+).
            //   Esta velocidad tangencial fue calibrada para generar una órbita estable.
            //   Es como "lanzar" el planeta perpendicular a la línea sol-planeta.
            // Masa: 180 (ligero comparado con el sol).
            // Radio: 10px.
            // Color: azul.
            new Body(
                position: new Vector2(420f, 0f),
                velocity: new Vector2(0f, 95f),
                mass: 180f,
                radius: 10f,
                color: new Color(0, 0, 255, 255)), // Azul para el planeta principal

            // ===== CUERPO 2: PLANETA VERDE =====
            // Posición: 360px hacia arriba (eje Y+).
            // Velocidad: 100px/s a la izquierda (eje X-).
            //   Nuevamente, perpendicular a la línea sol-planeta para órbita circular.
            // Masa: 140 (un poco más ligero que el planeta azul).
            // Radio: 8px.
            // Color: verde.
            new Body(
                position: new Vector2(0f, 360f),
                velocity: new Vector2(-100f, 0f),
                mass: 140f,
                radius: 8f,
                color: new Color(0, 255, 0, 255)), // Verde para un planeta secundario

            // ===== CUERPO 3: PLANETA MAGENTA =====
            // Posición: 560px a la izquierda (eje X-).
            // Velocidad: 82px/s hacia abajo (eje Y-).
            // Masa: 110.
            // Radio: 7px.
            // Color: magenta.
            new Body(
                position: new Vector2(-560f, 0f),
                velocity: new Vector2(0f, -82f),
                mass: 110f,
                radius: 7f,
                color: new Color(255, 0, 255, 255)), // Magenta para otro planeta

            // ===== CUERPO 4: PLANETA NARANJA =====
            // Posición: 470px hacia abajo (eje Y-).
            // Velocidad: 90px/s a la derecha (eje X+).
            // Masa: 100 (el más ligero de los planetas).
            // Radio: 6px.
            // Color: naranja.
            new Body(
                position: new Vector2(0f, -470f),
                velocity: new Vector2(90f, 0f),
                mass: 100f,
                radius: 6f,
                color: new Color(255, 165, 0, 255)) // Naranja para el cuarto planeta
        };
    }
}