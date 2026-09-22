using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Aether_NBody_Simulation;
using Forms = System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Aether_ControlPanel;

public partial class MainWindow : Window
{
    private Process? raylibProcess;
    private readonly Forms.Panel raylibPanel = new() { Dock = Forms.DockStyle.Fill, BackColor = System.Drawing.Color.Black };
    private readonly DispatcherTimer selectionTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly string projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Aether_NBody_Simulation", "Aether_NBody_Simulation.csproj"));

    public MainWindow()
    {
        InitializeComponent();
        RaylibHost.Child = raylibPanel;
        raylibPanel.Resize += (_, _) => ResizeEmbeddedRaylib();
        BodyTypeBox.ItemsSource = new[] { "Planet", "Star", "BlackHole", "Pulsar", "Quasar" };
        BodyTypeBox.SelectedIndex = 0;
        SceneBox.ItemsSource = new[]
        {
            new SceneOption("Phase 1: galaxies", "Universo Fase 1: galaxias"),
            new SceneOption("Phase 2: Big Bang", "Universo Fase 2: Big Bang"),
            new SceneOption("Test solar system", "Sistema de prueba"),
            new SceneOption("Chaotic three-body", "Tres cuerpos caótico"),
            new SceneOption("Binary stars", "Sistema binario"),
            new SceneOption("Asteroid belt", "Cinturón de asteroides"),
            new SceneOption("Orbital ring", "Anillo orbital"),
            new SceneOption("Wide spiral galaxy", "Galaxia Espiral Amplia"),
            new SceneOption("Realistic spiral galaxy", "Galaxia Espiral Realista"),
            new SceneOption("Galaxy collision", "Colisión de galaxias"),
            new SceneOption("Black hole test", "Agujero negro: espaguetificación"),
            new SceneOption("Pulsar 2D", "Púlsar 2D")
        };
        SceneBox.SelectedIndex = 0;
        selectionTimer.Tick += (_, _) => RefreshSelectionInfo();
        selectionTimer.Start();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (raylibProcess is null || raylibProcess.HasExited)
        {
            // El visor se inicia como ventana hija y se incrusta en el viewport WPF.
            raylibProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" -- --external-ui --hosted",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            StatusText.Text = "Raylib viewer started. Sending commands...";
            await EmbedRaylibWindowAsync(raylibProcess!);
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(250);
        StartButton_Click(sender, e);
    }

    private async void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        await SendAsync("pause");
    }

    private async void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpeedLabel is not null)
        {
            SpeedLabel.Text = $"{SpeedSlider.Value:0.###}x";
        }

        if (IsLoaded)
        {
            await SendAsync("speed", SpeedSlider.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private async void LoadSceneButton_Click(object sender, RoutedEventArgs e)
    {
        if (SceneBox.SelectedItem is SceneOption scene)
        {
            await SendAsync("scene", scene.CommandName);
        }
    }

    private async void SpawnButton_Click(object sender, RoutedEventArgs e)
    {
        await SendAsync(
            "spawn",
            BodyTypeBox.SelectedItem?.ToString() ?? "Planet",
            MassBox.Text,
            RadiusBox.Text,
            XBox.Text,
            YBox.Text);
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        await SendAsync("delete-selected");
    }

    private async void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        await SendAsync("select", SelectXBox.Text, SelectYBox.Text);
    }

    private void RefreshSelectionInfo()
    {
        string path = Path.Combine(Environment.CurrentDirectory, "aether.selection");
        if (File.Exists(path))
        {
            SelectedInfoText.Text = File.ReadAllText(path);
        }
    }

    private async Task EmbedRaylibWindowAsync(Process process)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            await Task.Delay(100);
            process.Refresh();
            IntPtr handle = process.MainWindowHandle;
            if (handle == IntPtr.Zero)
            {
                handle = FindMainWindow(process.Id);
            }

            if (handle != IntPtr.Zero && raylibPanel.IsHandleCreated)
            {
                NativeMethods.SetParent(handle, raylibPanel.Handle);
                int style = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_STYLE);
                NativeMethods.SetWindowLong(handle, NativeMethods.GWL_STYLE, (style | NativeMethods.WS_CHILD) & ~NativeMethods.WS_POPUP);
                NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
                ResizeEmbeddedRaylib(handle);
                StatusText.Text = "Connected — Raylib is embedded in this window.";
                return;
            }
        }

        StatusText.Text = "Raylib did not expose a window handle.";
    }

    private void ResizeEmbeddedRaylib()
    {
        if (raylibProcess is null || raylibProcess.HasExited)
        {
            return;
        }

        raylibProcess.Refresh();
        IntPtr handle = raylibProcess.MainWindowHandle;
        if (handle != IntPtr.Zero)
        {
            ResizeEmbeddedRaylib(handle);
        }
    }

    private void ResizeEmbeddedRaylib(IntPtr handle)
    {
        NativeMethods.MoveWindow(handle, 0, 0, raylibPanel.ClientSize.Width, raylibPanel.ClientSize.Height, true);
    }

    private static IntPtr FindMainWindow(int processId)
    {
        IntPtr result = IntPtr.Zero;
        NativeMethods.EnumWindows((handle, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(handle, out uint ownerId);
            if (ownerId == processId && NativeMethods.IsWindowVisible(handle))
            {
                result = handle;
                return false;
            }

            return true;
        }, IntPtr.Zero);
        return result;
    }

    private static async Task SendAsync(params string[] parts)
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(".", ControlPipeServer.PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(300);
            await using var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: false) { AutoFlush = true };
            await writer.WriteLineAsync(string.Join('|', parts));
        }
        catch
        {
            // El visor puede no haberse iniciado todavía; el usuario puede pulsar Start de nuevo.
        }
    }

    private sealed record SceneOption(string CommandName, string DisplayName)
    {
        public string Name => DisplayName;
    }

    private static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int WS_CHILD = 0x40000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int SW_SHOW = 5;

        [DllImport("user32.dll")] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
        public delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);
    }
}
