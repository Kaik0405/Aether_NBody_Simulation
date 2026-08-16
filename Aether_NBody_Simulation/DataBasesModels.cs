using System;

namespace Aether_NBody_Simulation;

// Representa una simulación guardada (ej. "Sistema Solar")
public sealed class SimulationRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Representa una fila en la tabla de cuerpos
public sealed class BodyRecord
{
    public int Id { get; set; }
    public int SimulationId { get; set; }
    public float Mass { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float VelX { get; set; }
    public float VelY { get; set; }
    public float Radius { get; set; }
    public bool IsStatic { get; set; }

    public byte ColorR { get; set; }
    public byte ColorG { get; set; }
    public byte ColorB { get; set; }
    public byte ColorA { get; set; }
}