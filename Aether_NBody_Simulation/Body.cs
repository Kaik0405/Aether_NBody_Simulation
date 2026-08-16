using System.Numerics;
using Raylib_cs;

namespace Aether_NBody_Simulation;

/// <summary>
/// Representa un cuerpo celeste genérico del simulador.
/// </summary>
/// <remarks>
/// La clase base conserva la API actual del motor, pero ahora puede especializarse en estrellas,
/// planetas, agujeros negros o asteroides para que el diseño sea más claro a futuro.
/// </remarks>
public class Body
{
    /// <summary>Posición actual del cuerpo en el espacio del mundo.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Velocidad actual del cuerpo.</summary>
    public Vector2 Velocity { get; set; }

    /// <summary>Aceleración actual calculada por el motor.</summary>
    public Vector2 Acceleration { get; set; }

    /// <summary>Masa del cuerpo usada por la gravedad.</summary>
    public float Mass { get; set; }

    /// <summary>Radio visual y de interacción del cuerpo.</summary>
    public float Radius { get; set; }

    /// <summary>Color visual del cuerpo.</summary>
    public Color Color { get; set; }

    /// <summary>Tipo semántico del cuerpo para distinguir su rol.</summary>
    public BodyKind Kind { get; protected set; }

    /// <summary>Nombre visual del tipo para mostrarlo en la UI o logs.</summary>
    public virtual string TypeName => Kind.ToString();

    /// <summary>Puntos recientes de la trayectoria del cuerpo.</summary>
    public List<Vector2> TrailPoints { get; } = new();

    /// <summary>Número máximo de puntos guardados en la estela.</summary>
    public int MaxTrailPoints { get; set; } = 500;
    /// <summary>Determina si el cuerpo es estatico o no </summary>
    public bool IsStatic { get; set; }

    /// <summary>Inicializa un cuerpo con los datos base.</summary>
    public Body(Vector2 position, Vector2 velocity, float mass, float radius, Color color, BodyKind kind, bool isStatic)
    {
        Position = position;
        Velocity = velocity;
        Acceleration = Vector2.Zero;
        Mass = mass;
        Radius = radius;
        Color = color;
        Kind = kind;
        IsStatic = isStatic;
    }

    /// <summary>Crea una copia del cuerpo manteniendo su estado actual.</summary>
    public virtual Body CloneBody()
    {
        return new Body(Position, Velocity, Mass, Radius, Color, Kind, IsStatic)
        {
            Acceleration = Acceleration,
            MaxTrailPoints = MaxTrailPoints
        };
    }

    /// <summary>Registra la posición actual en la estela del cuerpo.</summary>
    public void RecordTrailPoint()
    {
        TrailPoints.Add(Position);

        // Mantiene la estela acotada para evitar crecimiento infinito y conservar rendimiento.
        if (TrailPoints.Count > MaxTrailPoints)
        {
            TrailPoints.RemoveAt(0);
        }
    }
}

/// <summary>Un cuerpo con comportamiento de estrella: suele ser la fuente central del sistema.</summary>
public sealed class StarBody : Body
{
    public StarBody(Vector2 position, Vector2 velocity, float mass, float radius, Color color,bool isStatic)
        : base(position, velocity, mass, radius, color, BodyKind.Star,isStatic)
    {
    }

    public override Body CloneBody()
    {
        return new StarBody(Position, Velocity, Mass, Radius, Color,IsStatic)
        {
            Acceleration = Acceleration,
            MaxTrailPoints = MaxTrailPoints
        };
    }
}

/// <summary>Un cuerpo con comportamiento de planeta: orbita normalmente alrededor de una estrella.</summary>
public sealed class PlanetBody : Body
{
    public PlanetBody(Vector2 position, Vector2 velocity, float mass, float radius, Color color, bool isStatic)
        : base(position, velocity, mass, radius, color, BodyKind.Planet,isStatic)
    {
    }

    public override Body CloneBody()
    {
        return new PlanetBody(Position, Velocity, Mass, Radius, Color, IsStatic)
        {
            Acceleration = Acceleration,
            MaxTrailPoints = MaxTrailPoints
        };
    }
}

/// <summary>Un cuerpo de alto impacto gravitatorio, útil para sistemas extremos o caóticos.</summary>
public sealed class BlackHoleBody : Body
{
    public BlackHoleBody(Vector2 position, Vector2 velocity, float mass, float radius, Color color,bool isStatic)
        : base(position, velocity, mass, radius, color, BodyKind.BlackHole, isStatic)
    {
    }

    public override Body CloneBody()
    {
        return new BlackHoleBody(Position, Velocity, Mass, Radius, Color, IsStatic)
        {
            Acceleration = Acceleration,
            MaxTrailPoints = MaxTrailPoints
        };
    }
}

/// <summary>Un cuerpo pequeño, normalmente usado para cinturones de asteroides o restos.</summary>
public sealed class AsteroidBody : Body
{
    public AsteroidBody(Vector2 position, Vector2 velocity, float mass, float radius, Color color, bool isStatic)
        : base(position, velocity, mass, radius, color, BodyKind.Asteroid, isStatic)
    {
    }

    public override Body CloneBody()
    {
        return new AsteroidBody(Position, Velocity, Mass, Radius, Color, IsStatic)
        {
            Acceleration = Acceleration,
            MaxTrailPoints = MaxTrailPoints
        };
    }
}