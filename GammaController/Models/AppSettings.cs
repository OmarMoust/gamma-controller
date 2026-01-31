namespace GammaController.Models;

public class AppSettings
{
    public Dictionary<string, MonitorSettings> MonitorSettings { get; set; } = new();
    public bool RunAtStartup { get; set; } = false;
    public bool ApplyOnConnect { get; set; } = true;
    public string? LastSelectedMonitorSerial { get; set; }
}

public class MonitorSettings
{
    public string FriendlyName { get; set; } = string.Empty;
    public double Gamma { get; set; } = 1.0;
}

