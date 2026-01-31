using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GammaController.Helpers;
using GammaController.Models;
using GammaController.Services;

namespace GammaController;

public partial class MainWindow : Window
{
    private readonly GammaService _gammaService;
    private readonly SettingsService _settingsService;
    private readonly string _logPath;
    private List<MonitorInfo> _monitors = new();
    private MonitorInfo? _selectedMonitor;
    private bool _isUpdatingSlider;
    private bool _isClosing;
    private bool _isDragging;

    public MainWindow(GammaService gammaService, SettingsService settingsService)
    {
        _gammaService = gammaService;
        _settingsService = settingsService;
        
        _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GammaController", "debug.log");
        
        Log("MainWindow constructor called");
        
        InitializeComponent();
        
        // Set accent color
        var accentColor = AccentColorHelper.GetAccentColor();
        Resources["AccentBrush"] = new SolidColorBrush(accentColor);
        
        // Hook up slider drag events
        GammaSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(Slider_DragStarted));
        GammaSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(Slider_DragCompleted));
        
        Log("MainWindow initialized");
    }
    
    private void Log(string message)
    {
        try { File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss}] [UI] {message}\n"); } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Log("Window_Loaded called");
        
        // Apply Windows 11 effects
        WindowEffects.EnableAcrylic(this);
        WindowEffects.MakeToolWindow(this);
        
        RefreshMonitors();
    }

    public void RefreshMonitors()
    {
        Log("RefreshMonitors called");
        _monitors = _gammaService.GetMonitors();
        Log($"Found {_monitors.Count} monitors");
        
        // Clear existing buttons
        MonitorButtonsPanel.Children.Clear();
        
        // Show buttons only if multiple monitors
        MonitorButtonsPanel.Visibility = _monitors.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

        if (_monitors.Count > 1)
        {
            // Create segmented buttons for each monitor
            for (int i = 0; i < _monitors.Count; i++)
            {
                var monitor = _monitors[i];
                var button = new RadioButton
                {
                    Content = monitor.DisplayName,
                    Tag = i,
                    GroupName = "Monitors",
                    Style = (Style)Resources["MonitorButtonStyle"],
                    Margin = new Thickness(i == 0 ? 0 : 4, 0, 0, 0)
                };
                button.Checked += MonitorButton_Checked;
                MonitorButtonsPanel.Children.Add(button);
            }
        }

        // Select previous or first monitor
        if (_monitors.Count > 0)
        {
            var lastSerial = _settingsService.Settings.LastSelectedMonitorSerial;
            var index = _monitors.FindIndex(m => m.SerialNumber == lastSerial);
            
            if (index < 0) index = 0;
            
            // Check the corresponding button
            if (_monitors.Count > 1 && MonitorButtonsPanel.Children.Count > index)
            {
                ((RadioButton)MonitorButtonsPanel.Children[index]).IsChecked = true;
            }
            
            SelectMonitor(index);
        }
    }

    private void MonitorButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton button && button.Tag is int index)
        {
            SelectMonitor(index);
        }
    }

    private void SelectMonitor(int index)
    {
        if (index < 0 || index >= _monitors.Count) return;
        
        _selectedMonitor = _monitors[index];
        _settingsService.Settings.LastSelectedMonitorSerial = _selectedMonitor.SerialNumber;
        _settingsService.Save();
        
        // Update slider to current gamma
        _isUpdatingSlider = true;
        
        // Get saved gamma for this monitor, or read current
        double gamma;
        if (!string.IsNullOrEmpty(_selectedMonitor.SerialNumber) && 
            _settingsService.IsMonitorKnown(_selectedMonitor.SerialNumber))
        {
            gamma = _settingsService.GetMonitorGamma(_selectedMonitor.SerialNumber);
        }
        else
        {
            gamma = _gammaService.GetGamma(_selectedMonitor);
        }
        
        GammaSlider.Value = Math.Clamp(gamma, 0.5, 2.0);
        _isUpdatingSlider = false;
    }

    private void GammaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Update the gamma value display (convert to 0-100 scale for readability)
        if (GammaValueText != null)
        {
            int displayValue = (int)Math.Round((e.NewValue - 0.5) / 1.5 * 100);
            GammaValueText.Text = displayValue.ToString();
            
            // Position the tooltip to follow the thumb
            UpdateTooltipPosition();
        }
        
        if (_logPath == null) return; // Not fully initialized yet
        
        Log($"Slider value changed: {e.OldValue} -> {e.NewValue}, isUpdating={_isUpdatingSlider}, monitor={_selectedMonitor?.DisplayName ?? "null"}");
        
        if (_isUpdatingSlider || _selectedMonitor == null) return;
        
        double gamma = Math.Round(e.NewValue, 2);
        
        Log($"Setting gamma to {gamma} on {_selectedMonitor.DisplayName} ({_selectedMonitor.DeviceName})");
        
        // Apply gamma in real-time
        bool success = _gammaService.SetGamma(_selectedMonitor, gamma);
        Log($"SetGamma result: {success}");
        
        // Save setting
        if (!string.IsNullOrEmpty(_selectedMonitor.SerialNumber))
        {
            _settingsService.SetMonitorGamma(
                _selectedMonitor.SerialNumber,
                _selectedMonitor.DisplayName,
                gamma);
        }
    }
    
    private void UpdateTooltipPosition()
    {
        if (GammaSlider.ActualWidth <= 0 || GammaTooltip == null) return;
        
        // The slider track is narrower than the slider itself because of thumb width (20px)
        double thumbWidth = 20;
        double trackWidth = GammaSlider.ActualWidth - thumbWidth;
        double percent = (GammaSlider.Value - GammaSlider.Minimum) / (GammaSlider.Maximum - GammaSlider.Minimum);
        double thumbCenterX = (thumbWidth / 2) + (percent * trackWidth);
        
        // Measure tooltip width
        GammaTooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double tooltipWidth = GammaTooltip.DesiredSize.Width;
        
        // Center the tooltip over the thumb
        Canvas.SetLeft(GammaTooltip, thumbCenterX - (tooltipWidth / 2));
    }
    
    private void Slider_DragStarted(object sender, DragStartedEventArgs e)
    {
        _isDragging = true;
        GammaTooltip.Visibility = Visibility.Visible;
        UpdateTooltipPosition();
    }
    
    private void Slider_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        _isDragging = false;
        GammaTooltip.Visibility = Visibility.Collapsed;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Hide window when it loses focus (like Windows volume flyout)
        HideWithAnimation();
    }

    public void ShowNearTray()
    {
        _isClosing = false;
        
        // Calculate position above the taskbar
        var workArea = SystemParameters.WorkArea;
        
        // Measure the window first
        Measure(new Size(Width, double.PositiveInfinity));
        var desiredHeight = DesiredSize.Height > 0 ? DesiredSize.Height : 80;
        
        // Position: right side, well above taskbar
        Left = workArea.Right - Width - 12;
        Top = workArea.Bottom - desiredHeight - 50;
        
        Show();
        Activate();
        
        // Play slide-in animation
        var storyboard = (Storyboard)Resources["SlideInAnimation"];
        storyboard.Begin(this);
    }

    private void HideWithAnimation()
    {
        if (_isClosing) return;
        _isClosing = true;
        
        var storyboard = (Storyboard)Resources["SlideOutAnimation"];
        storyboard.Completed += (_, _) => 
        {
            if (_isClosing) Hide();
        };
        storyboard.Begin(this);
    }

    public void ToggleVisibility()
    {
        if (IsVisible && !_isClosing)
        {
            HideWithAnimation();
        }
        else
        {
            RefreshMonitors();
            ShowNearTray();
        }
    }
}
