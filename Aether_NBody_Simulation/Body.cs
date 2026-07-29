using System.Numerics;
using Raylib_cs;

namespace Aether_NBody_Simulation;

/// <summary>
/// Representa un cuerpo puntual del simulador.
/// </summary>
/// <remarks>
/// Esta clase está pensada para crecer sin romper la API pública:
/// en el futuro podrá participar en RK4, Barnes-Hut, multithreading y telemetría.
/// </remarks>
public sealed class Body
{
    /// <summary>
    /// Posición actual en coordenadas del mundo.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Velocidad actual del cuerpo.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Aceleración actual calculada por el motor físico.
    /// </summary>
    public Vector2 Acceleration { get; set; }

    /// <summary>
    /// Masa del cuerpo, usada para el cálculo gravitatorio.
    /// </summary>
    public float Mass { get; set; }

    /// <summary>
    /// Radio visual/físico del cuerpo.
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// Color visual del cuerpo.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Puntos recientes de la trayectoria del cuerpo para dibujar una estela.
    /// </summary>
    public List<Vector2> TrailPoints { get; } = new();

    /// <summary>
    /// Número máximo de puntos que se guardan en la estela.
    /// Con 500 puntos a 60 FPS, la estela cubre aproximadamente 8.3 segundos de historia.
    /// Esto es suficiente para ver casi una órbita completa en la mayoría de casos.
    /// </summary>
    public int MaxTrailPoints { get; set; } = 500;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="Body"/>.
    /// </summary>
    /// <param name="position">Posición inicial.</param>
    /// <param name="velocity">Velocidad inicial.</param>
    /// <param name="mass">Masa del cuerpo.</param>
    /// <param name="radius">Radio visual/físico del cuerpo.</param>
    public Body(Vector2 position, Vector2 velocity, float mass, float radius, Color color)
    {
        Position = position;
        Velocity = velocity;
        Acceleration = Vector2.Zero;
        Mass = mass;
        Radius = radius;
        Color = color;
    }

    public Body CloneBody()
    {
        return new Body(Position, Velocity, Mass, Radius, Color);
    }

    /// <summary>
    /// Registra la posición actual en la estela del cuerpo.
    /// </summary>
    /// <remarks>
    /// Se llama una vez por frame desde PhysicsEngine.Update().
    /// Mantiene un histórico de posiciones recientes que se dibuja como una línea
    /// blanca semi-transparente (estela) en cada paso de render.
    /// </remarks>
    public void RecordTrailPoint()
    {
        TrailPoints.Add(Position);

        // Si la estela supera el máximo, eliminamos el punto más antiguo (estructura FIFO).
        // Esto mantiene un tamaño constante y evita gastar memoria sin límite.
        if (TrailPoints.Count > MaxTrailPoints)
        {
            TrailPoints.RemoveAt(0);
        }
    }
}