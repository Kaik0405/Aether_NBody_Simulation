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
    
    /// <summary>Radio inicial usado como referencia durante la deformación.</summary>
    public float OriginalRadius { get; }
    
    /// <summary>Masa inicial conservada para que la absorción no destruya la definición del cuerpo.</summary>
    public float OriginalMass { get; }

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

    /// <summary>Indica que el cuerpo está siendo absorbido y debe mostrar una deformación visual.</summary>
    public bool IsBeingAbsorbed { get; set; }

    /// <summary>Progreso de la absorción: 0 es normal y 1 es el horizonte de sucesos.</summary>
    public float AbsorptionProgress { get; set; }

    /// <summary>Dirección desde el cuerpo hacia el agujero negro que lo está absorbiendo.</summary>
    public Vector2 AbsorptionDirection { get; set; }

    /// <summary>Indica que el cuerpo ha entrado en la fase de pixelación final.</summary>
    public bool IsPixelating { get; set; }
    
    /// <summary>Progreso de pixelación antes de desaparecer.</summary>
    public float PixelationProgress { get; set; }

    /// <summary>Indica que el cuerpo ya desapareció del render y de la simulación.</summary>
    public bool IsConsumed { get; set; }

    /// <summary>Ángulo usado por las animaciones de rotación de pulsares y discos.</summary>
    public float VisualRotation { get; set; }

    /// <summary>Inicializa un cuerpo con los datos base.</summary>
    public Body(Vector2 position, Vector2 velocity, float mass, float radius, Color color, BodyKind kind, bool isStatic)
    {
        Position = position;
        Velocity = velocity;
        Acceleration = Vector2.Zero;
        Mass = mass;
        OriginalMass = mass;
        Radius = radius;
        OriginalRadius = radius;
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
            MaxTrailPoints = MaxTrailPoints,
            IsBeingAbsorbed = IsBeingAbsorbed,
            AbsorptionProgress = AbsorptionProgress,
            AbsorptionDirection = AbsorptionDirection,
                IsPixelating = IsPixelating,
                PixelationProgress = PixelationProgress,
                IsConsumed = IsConsumed,
                VisualRotation = VisualRotation
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
            MaxTrailPoints = MaxTrailPoints,
            IsBeingAbsorbed = IsBeingAbsorbed,
            AbsorptionProgress = AbsorptionProgress,
            AbsorptionDirection = AbsorptionDirection,
            IsPixelating = IsPixelating,
            PixelationProgress = PixelationProgress,
            IsConsumed = IsConsumed,
            VisualRotation = VisualRotation
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
            MaxTrailPoints = MaxTrailPoints,
            IsBeingAbsorbed = IsBeingAbsorbed,
            AbsorptionProgress = AbsorptionProgress,
            AbsorptionDirection = AbsorptionDirection,
            IsPixelating = IsPixelating,
            PixelationProgress = PixelationProgress,
            IsConsumed = IsConsumed,
            VisualRotation = VisualRotation
        };
    }
}

/// <summary>Un cuerpo de alto impacto gravitatorio, útil para sistemas extremos o caóticos.</summary>
public class BlackHoleBody : Body
{
    /// <summary>Radio de influencia visual donde comienzan las mareas gravitatorias.</summary>
    public float TidalRadius { get; set; } = 600f;

    /// <summary>Radio visual de absorción inmediata, equivalente artístico al horizonte de sucesos.</summary>
    public float EventHorizonRadius { get; set; } = 55f;

    public BlackHoleBody(Vector2 position, Vector2 velocity, float mass, float radius, Color color,bool isStatic)
        : base(position, velocity, mass, radius, color, BodyKind.BlackHole, isStatic)
    {
    }

    public override Body CloneBody()
    {
        return new BlackHoleBody(Position, Velocity, Mass, Radius, Color, IsStatic)
        {
            Acceleration = Acceleration,
            MaxTrailPoints = MaxTrailPoints,
            TidalRadius = TidalRadius,
            EventHorizonRadius = EventHorizonRadius,
            IsBeingAbsorbed = IsBeingAbsorbed,
            AbsorptionProgress = AbsorptionProgress,
            AbsorptionDirection = AbsorptionDirection,
            IsPixelating = IsPixelating,
            PixelationProgress = PixelationProgress,
            IsConsumed = IsConsumed,
            VisualRotation = VisualRotation
        };
    }
}

/// <summary>Un agujero negro supermasivo que actúa como núcleo de una galaxia y tiene jets de acreción.</summary>
public sealed class QuasarBody : BlackHoleBody
{
    public float JetLength { get; set; } = 180f;
    public float JetWidth { get; set; } = 22f;

    public QuasarBody(Vector2 position, Vector2 velocity, float mass, float radius, Color color, bool isStatic)
        : base(position, velocity, mass, radius, color, isStatic)
    {
        Kind = BodyKind.Quasar;
        TidalRadius = 900f;
        EventHorizonRadius = radius * 1.3f;
        JetLength = radius * 4f;
        JetWidth = radius * 0.55f;
    }

    public override Body CloneBody()
    {
        return new QuasarBody(Position, Velocity, Mass, Radius, Color, IsStatic)
        {
            Acceleration = Acceleration,
            MaxTrailPoints = MaxTrailPoints,
            TidalRadius = TidalRadius,
            EventHorizonRadius = EventHorizonRadius,
            JetLength = JetLength,
            JetWidth = JetWidth,
            IsBeingAbsorbed = IsBeingAbsorbed,
            AbsorptionProgress = AbsorptionProgress,
            AbsorptionDirection = AbsorptionDirection,
            IsPixelating = IsPixelating,
            PixelationProgress = PixelationProgress,
            IsConsumed = IsConsumed,
            VisualRotation = VisualRotation
        };
    }
}

/// <summary>Estrella de neutrones que muestra dos haces de plasma girando en 2D.</summary>
public sealed class PulsarBody : Body
{
    public float SpinSpeed { get; set; } = 5f;
    public float JetLength { get; set; } = 80f;

    public PulsarBody(Vector2 position, Vector2 velocity, float mass, float radius, Color color, bool isStatic)
        : base(position, velocity, mass, radius, color, BodyKind.Pulsar, isStatic)
    {
    }

    public override Body CloneBody()
    {
        return new PulsarBody(Position, Velocity, Mass, Radius, Color, IsStatic)
        {
            Acceleration = Acceleration,
            MaxTrailPoints = MaxTrailPoints,
            SpinSpeed = SpinSpeed,
            JetLength = JetLength,
            IsBeingAbsorbed = IsBeingAbsorbed,
            AbsorptionProgress = AbsorptionProgress,
            AbsorptionDirection = AbsorptionDirection,
            IsPixelating = IsPixelating,
            PixelationProgress = PixelationProgress,
            IsConsumed = IsConsumed,
            VisualRotation = VisualRotation
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
            MaxTrailPoints = MaxTrailPoints,
            IsBeingAbsorbed = IsBeingAbsorbed,
            AbsorptionProgress = AbsorptionProgress,
            AbsorptionDirection = AbsorptionDirection,
            IsPixelating = IsPixelating,
            PixelationProgress = PixelationProgress,
            IsConsumed = IsConsumed,
            VisualRotation = VisualRotation
        };
    }
}