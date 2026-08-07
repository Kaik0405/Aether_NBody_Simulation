using System.Numerics;
using Raylib_cs;

namespace Aether_NBody_Simulation;

/// <summary>
/// Describe el tipo de cuerpo que representa una entidad del universo.
/// </summary>
public enum BodyKind
{
    Generic,
    Star,
    Planet,
    BlackHole,
    Asteroid
}

/// <summary>
/// Contenedor base para una escena astronómica: sistema solar, galaxia o configuración de prueba.
/// </summary>
public abstract class SimulationScene
{
    protected SimulationScene(string name, string description, IReadOnlyList<Body> bodies)
    {
        Name = name;
        Description = description;
        Bodies = bodies;
    }

    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<Body> Bodies { get; }
}

/// <summary>
/// Escena simple orientada a un sistema solar o a un sistema compacto.
/// </summary>
public sealed class SolarSystemScene : SimulationScene
{
    public SolarSystemScene(string name, string description, IReadOnlyList<Body> bodies)
        : base(name, description, bodies)
    {
    }
}

/// <summary>
/// Escena orientada a estructuras de mayor escala, como una galaxia.
/// </summary>
public sealed class GalaxyScene : SimulationScene
{
    public GalaxyScene(string name, string description, IReadOnlyList<Body> bodies)
        : base(name, description, bodies)
    {
    }
}
