namespace GammaController.Models;

public class MonitorInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string DevicePath { get; set; } = string.Empty;
    public IntPtr DeviceContext { get; set; } = IntPtr.Zero;
    public bool IsPrimary { get; set; }
    
    public string DisplayName => string.IsNullOrEmpty(FriendlyName) 
        ? DeviceName 
        : FriendlyName;
    
    public override string ToString() => DisplayName;
}

