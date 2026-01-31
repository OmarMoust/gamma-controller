using System.Drawing;
using System.Windows;
using GammaController.Services;
using Hardcodet.Wpf.TaskbarNotification;

namespace GammaController;

public partial class App : Application
{
    private SingleInstanceService? _singleInstance;
    private GammaService? _gammaService;
    private SettingsService? _settingsService;
    private MonitorWatcher? _monitorWatcher;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private bool _startMinimized;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _startMinimized = e.Args.Contains("--minimized");

        // Single instance check
        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.TryAcquireLock())
        {
            Shutdown();
            return;
        }

        _singleInstance.ShowWindowRequested += (_, _) => 
            Dispatcher.Invoke(() => _mainWindow?.ToggleVisibility());
        _singleInstance.StartListening();

        // Initialize services
        _gammaService = new GammaService();
        _settingsService = new SettingsService();
        
        // Sync startup setting with registry
        _settingsService.Settings.RunAtStartup = _settingsService.IsStartupEnabled();

        // Create main window
        _mainWindow = new MainWindow(_gammaService, _settingsService);

        // Setup monitor watcher
        _monitorWatcher = new MonitorWatcher(_gammaService);
        _monitorWatcher.MonitorConnected += OnMonitorConnected;
        _monitorWatcher.MonitorsChanged += (_, _) => 
            Dispatcher.Invoke(() => _mainWindow?.RefreshMonitors());
        _monitorWatcher.Start();

        // Create tray icon
        CreateTrayIcon();

        // Show window if not starting minimized
        if (!_startMinimized)
        {
            _mainWindow.ShowNearTray();
        }
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            Icon = CreateIcon(),
            ToolTipText = "Gamma Controller",
            ContextMenu = CreateContextMenu()
        };

        _trayIcon.TrayLeftMouseUp += (_, _) => _mainWindow?.ToggleVisibility();
    }

    private System.Drawing.Icon CreateIcon()
    {
        // Create a simple sun icon programmatically
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        
        // Draw sun circle
        using var brush = new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 200, 50));
        g.FillEllipse(brush, 8, 8, 16, 16);
        
        // Draw rays
        using var pen = new Pen(System.Drawing.Color.FromArgb(255, 255, 200, 50), 2);
        g.DrawLine(pen, 16, 2, 16, 6);
        g.DrawLine(pen, 16, 26, 16, 30);
        g.DrawLine(pen, 2, 16, 6, 16);
        g.DrawLine(pen, 26, 16, 30, 16);
        g.DrawLine(pen, 6, 6, 9, 9);
        g.DrawLine(pen, 23, 23, 26, 26);
        g.DrawLine(pen, 26, 6, 23, 9);
        g.DrawLine(pen, 6, 26, 9, 23);
        
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        // Run at startup
        var startupItem = new System.Windows.Controls.MenuItem
        {
            Header = "Run at startup",
            IsCheckable = true,
            IsChecked = _settingsService?.Settings.RunAtStartup ?? false
        };
        startupItem.Click += (_, _) =>
        {
            if (_settingsService != null)
            {
                _settingsService.RunAtStartup = startupItem.IsChecked;
            }
        };
        menu.Items.Add(startupItem);

        // Apply on connect
        var applyOnConnectItem = new System.Windows.Controls.MenuItem
        {
            Header = "Apply gamma on connect",
            IsCheckable = true,
            IsChecked = _settingsService?.Settings.ApplyOnConnect ?? true
        };
        applyOnConnectItem.Click += (_, _) =>
        {
            if (_settingsService != null)
            {
                _settingsService.ApplyOnConnect = applyOnConnectItem.IsChecked;
            }
        };
        menu.Items.Add(applyOnConnectItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        // Reset gamma
        var resetItem = new System.Windows.Controls.MenuItem { Header = "Reset all to default" };
        resetItem.Click += (_, _) =>
        {
            if (_gammaService == null) return;
            foreach (var monitor in _gammaService.GetMonitors())
            {
                _gammaService.ResetGamma(monitor);
            }
            _mainWindow?.RefreshMonitors();
        };
        menu.Items.Add(resetItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        // Exit
        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OnMonitorConnected(object? sender, MonitorChangedEventArgs e)
    {
        if (_settingsService == null || _gammaService == null) return;
        if (!_settingsService.Settings.ApplyOnConnect) return;
        
        var serial = e.Monitor.SerialNumber;
        if (string.IsNullOrEmpty(serial)) return;
        
        if (_settingsService.IsMonitorKnown(serial))
        {
            var gamma = _settingsService.GetMonitorGamma(serial);
            Dispatcher.Invoke(() =>
            {
                // Need to refresh monitors to get updated device context
                var monitors = _gammaService.GetMonitors();
                var monitor = monitors.FirstOrDefault(m => m.SerialNumber == serial);
                if (monitor != null)
                {
                    _gammaService.SetGamma(monitor, gamma);
                }
            });
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _monitorWatcher?.Dispose();
        _gammaService?.Dispose();
        _singleInstance?.Dispose();
        
        base.OnExit(e);
    }
}
