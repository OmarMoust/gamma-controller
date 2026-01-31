using System.Management;
using GammaController.Models;

namespace GammaController.Services;

public class MonitorWatcher : IDisposable
{
    private ManagementEventWatcher? _deviceWatcher;
    private readonly GammaService _gammaService;
    private HashSet<string> _knownMonitorSerials = new();
    private bool _disposed;

    public event EventHandler<MonitorChangedEventArgs>? MonitorConnected;
    public event EventHandler<MonitorChangedEventArgs>? MonitorDisconnected;
    public event EventHandler? MonitorsChanged;

    public MonitorWatcher(GammaService gammaService)
    {
        _gammaService = gammaService;
    }

    public void Start()
    {
        // Initialize known monitors
        RefreshKnownMonitors();
        
        try
        {
            // Watch for device changes (USB, display, etc.)
            var query = new WqlEventQuery(
                "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
            
            _deviceWatcher = new ManagementEventWatcher(query);
            _deviceWatcher.EventArrived += OnDeviceChanged;
            _deviceWatcher.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start monitor watcher: {ex.Message}");
        }
    }

    private void RefreshKnownMonitors()
    {
        _knownMonitorSerials = _gammaService.GetMonitors()
            .Where(m => !string.IsNullOrEmpty(m.SerialNumber))
            .Select(m => m.SerialNumber)
            .ToHashSet();
    }

    private async void OnDeviceChanged(object sender, EventArrivedEventArgs e)
    {
        // Debounce - wait a bit for the system to settle
        await Task.Delay(500);
        
        try
        {
            _gammaService.InvalidateDeviceContexts();
            var currentMonitors = _gammaService.GetMonitors();
            var currentSerials = currentMonitors
                .Where(m => !string.IsNullOrEmpty(m.SerialNumber))
                .Select(m => m.SerialNumber)
                .ToHashSet();

            // Check for new monitors
            foreach (var serial in currentSerials.Except(_knownMonitorSerials))
            {
                var monitor = currentMonitors.First(m => m.SerialNumber == serial);
                MonitorConnected?.Invoke(this, new MonitorChangedEventArgs(monitor));
            }

            // Check for removed monitors
            foreach (var serial in _knownMonitorSerials.Except(currentSerials))
            {
                MonitorDisconnected?.Invoke(this, new MonitorChangedEventArgs(new MonitorInfo { SerialNumber = serial }));
            }

            _knownMonitorSerials = currentSerials;
            MonitorsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling device change: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_deviceWatcher != null)
        {
            _deviceWatcher.Stop();
            _deviceWatcher.Dispose();
            _deviceWatcher = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}

public class MonitorChangedEventArgs : EventArgs
{
    public MonitorInfo Monitor { get; }
    
    public MonitorChangedEventArgs(MonitorInfo monitor)
    {
        Monitor = monitor;
    }
}

