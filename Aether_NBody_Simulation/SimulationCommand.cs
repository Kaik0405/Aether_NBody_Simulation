namespace Aether_NBody_Simulation;

/// <summary>Comando textual compartido entre el panel WPF y la ventana Raylib.</summary>
public sealed record SimulationCommand(string Name, string[] Arguments)
{
    public static SimulationCommand Parse(string line)
    {
        string[] parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0
            ? new SimulationCommand(string.Empty, Array.Empty<string>())
            : new SimulationCommand(parts[0], parts.Skip(1).ToArray());
    }
}
