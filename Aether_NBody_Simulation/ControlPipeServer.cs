using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Aether_NBody_Simulation;

/// <summary>Recibe comandos del panel externo sin bloquear el hilo de renderizado.</summary>
public sealed class ControlPipeServer : IDisposable
{
    public const string PipeName = "Aether_NBody_Simulation_Control";
    private readonly ConcurrentQueue<SimulationCommand> commands = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task listenerTask;

    public ControlPipeServer()
    {
        listenerTask = Task.Run(ListenAsync);
    }

    public bool TryDequeue(out SimulationCommand? command) => commands.TryDequeue(out command);

    private async Task ListenAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(cancellation.Token);
                using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
                while (pipe.IsConnected && !cancellation.IsCancellationRequested)
                {
                    string? line = await reader.ReadLineAsync(cancellation.Token);
                    if (line is null)
                    {
                        break;
                    }

                    commands.Enqueue(SimulationCommand.Parse(line));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // El panel puede cerrarse y reconectarse; el visor continúa funcionando.
            }
        }
    }

    public void Dispose()
    {
        cancellation.Cancel();
        try { listenerTask.Wait(500); } catch { }
        cancellation.Dispose();
    }
}
