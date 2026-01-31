using System.IO;
using System.Runtime.InteropServices;
using GammaController.Interop;
using GammaController.Models;

namespace GammaController.Services;

public class GammaService : IDisposable
{
    private readonly Dictionary<string, IntPtr> _deviceContexts = new();
    private readonly string _logPath;
    private bool _disposed;

    public GammaService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GammaController");
        Directory.CreateDirectory(appData);
        _logPath = Path.Combine(appData, "debug.log");
        
        // Clear old log
        try { File.WriteAllText(_logPath, $"GammaController started at {DateTime.Now}\n"); } catch { }
    }

    private void Log(string message)
    {
        try
        {
            File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(message);
    }

    public List<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        
        // Get WMI monitor info for serial numbers
        var wmiMonitors = GetWmiMonitorInfo();
        
        uint deviceIndex = 0;
        var displayDevice = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };

        while (NativeMethods.EnumDisplayDevices(null, deviceIndex, ref displayDevice, 0))
        {
            Log($"Device {deviceIndex}: {displayDevice.DeviceName} - State: {displayDevice.StateFlags:X}");
            
            if ((displayDevice.StateFlags & (uint)DisplayDeviceStateFlags.AttachedToDesktop) != 0)
            {
                var monitor = new MonitorInfo
                {
                    DeviceName = displayDevice.DeviceName,
                    IsPrimary = (displayDevice.StateFlags & (uint)DisplayDeviceStateFlags.PrimaryDevice) != 0,
                };
                
                // Get monitor device info
                var monitorDevice = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
                if (NativeMethods.EnumDisplayDevices(displayDevice.DeviceName, 0, ref monitorDevice, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME))
                {
                    monitor.DevicePath = monitorDevice.DeviceID;
                    Log($"  Monitor: {monitorDevice.DeviceString} - ID: {monitorDevice.DeviceID}");
                    
                    // Match with WMI data for friendly name and serial
                    var wmiMatch = wmiMonitors.FirstOrDefault(w => 
                        monitorDevice.DeviceID.Contains(w.InstancePath, StringComparison.OrdinalIgnoreCase));
                    
                    if (wmiMatch != null)
                    {
                        monitor.FriendlyName = wmiMatch.FriendlyName;
                        monitor.SerialNumber = wmiMatch.SerialNumber;
                        monitor.Manufacturer = wmiMatch.Manufacturer;
                    }
                    else
                    {
                        monitor.FriendlyName = monitorDevice.DeviceString;
                    }
                }
                else
                {
                    monitor.FriendlyName = displayDevice.DeviceString;
                }
                
                monitors.Add(monitor);
            }
            
            deviceIndex++;
        }
        
        Log($"Found {monitors.Count} monitors total");
        return monitors;
    }

    private List<WmiMonitorData> GetWmiMonitorInfo()
    {
        var result = new List<WmiMonitorData>();
        
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                @"root\wmi", 
                "SELECT * FROM WmiMonitorID");
            
            foreach (var obj in searcher.Get())
            {
                var data = new WmiMonitorData
                {
                    InstancePath = ExtractInstancePath(obj["InstanceName"]?.ToString() ?? ""),
                    FriendlyName = DecodeWmiString(obj["UserFriendlyName"]),
                    SerialNumber = DecodeWmiString(obj["SerialNumberID"]),
                    Manufacturer = DecodeWmiString(obj["ManufacturerName"])
                };
                
                Log($"WMI Monitor: {data.FriendlyName} - Serial: {data.SerialNumber} - Path: {data.InstancePath}");
                result.Add(data);
            }
        }
        catch (Exception ex)
        {
            Log($"WMI Error: {ex.Message}");
        }
        
        return result;
    }

    private static string ExtractInstancePath(string instanceName)
    {
        var parts = instanceName.Split('\\');
        if (parts.Length >= 2)
            return $"{parts[0]}\\{parts[1]}";
        return instanceName;
    }

    private static string DecodeWmiString(object? value)
    {
        if (value is ushort[] bytes)
        {
            var chars = bytes.TakeWhile(b => b != 0).Select(b => (char)b).ToArray();
            return new string(chars).Trim();
        }
        return string.Empty;
    }

    public double GetGamma(MonitorInfo monitor)
    {
        var hdc = GetDeviceContext(monitor.DeviceName);
        if (hdc == IntPtr.Zero) return 1.0;

        var ramp = new ushort[3, 256];

        if (!NativeMethods.GetDeviceGammaRamp(hdc, ramp))
            return 1.0;

        double midValue = ramp[0, 128] / 65535.0;
        if (midValue <= 0 || midValue >= 1) return 1.0;
        
        double gamma = Math.Log(0.5) / Math.Log(midValue);
        return Math.Round(Math.Clamp(gamma, 0.5, 2.0), 2);
    }

    public bool SetGamma(MonitorInfo monitor, double gamma)
    {
        Log($"SetGamma called for '{monitor.DeviceName}' with gamma {gamma}");
        
        // Try to get DC for specific device first
        var hdc = GetDeviceContext(monitor.DeviceName);
        
        if (hdc == IntPtr.Zero)
        {
            Log("Device-specific DC failed, trying screen DC...");
            // Fallback: try getting the screen DC
            hdc = NativeMethods.GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                Log("GetDC(NULL) also failed!");
                return false;
            }
            Log($"Got screen DC: {hdc}");
        }

        gamma = Math.Clamp(gamma, 0.4, 2.8);

        var ramp = new ushort[3, 256];

        for (int i = 0; i < 256; i++)
        {
            double normalizedInput = i / 255.0;
            double correctedValue = Math.Pow(normalizedInput, 1.0 / gamma);
            ushort gammaValue = (ushort)Math.Clamp(Math.Round(correctedValue * 65535.0), 0, 65535);
            
            ramp[0, i] = gammaValue;
            ramp[1, i] = gammaValue;
            ramp[2, i] = gammaValue;
        }

        Log($"Ramp sample - [0,0]: {ramp[0, 0]}, [0,128]: {ramp[0, 128]}, [0,255]: {ramp[0, 255]}");

        bool result = NativeMethods.SetDeviceGammaRamp(hdc, ramp);
        
        if (!result)
        {
            int error = Marshal.GetLastWin32Error();
            Log($"SetDeviceGammaRamp FAILED! Win32 Error: {error}");
            
            // Try alternative: use the primary screen DC
            if (hdc != _deviceContexts.GetValueOrDefault(monitor.DeviceName))
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }
        else
        {
            Log("SetDeviceGammaRamp SUCCEEDED!");
        }
        
        return result;
    }

    public void ResetGamma(MonitorInfo monitor)
    {
        SetGamma(monitor, 1.0);
    }

    private IntPtr GetDeviceContext(string deviceName)
    {
        if (_deviceContexts.TryGetValue(deviceName, out var existingDc) && existingDc != IntPtr.Zero)
        {
            Log($"Using cached DC for {deviceName}: {existingDc}");
            return existingDc;
        }

        // Try CreateDC with device name
        var hdc = NativeMethods.CreateDC(null, deviceName, null, IntPtr.Zero);
        
        if (hdc == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            Log($"CreateDC(null, '{deviceName}', null, 0) failed! Error: {error}");
            
            // Try with "DISPLAY" as driver
            hdc = NativeMethods.CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                error = Marshal.GetLastWin32Error();
                Log($"CreateDC('DISPLAY', '{deviceName}', null, 0) also failed! Error: {error}");
            }
        }
        
        if (hdc != IntPtr.Zero)
        {
            Log($"Created DC for {deviceName}: {hdc}");
            _deviceContexts[deviceName] = hdc;
        }
        
        return hdc;
    }

    public void InvalidateDeviceContexts()
    {
        foreach (var hdc in _deviceContexts.Values)
        {
            if (hdc != IntPtr.Zero)
                NativeMethods.DeleteDC(hdc);
        }
        _deviceContexts.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        InvalidateDeviceContexts();
        GC.SuppressFinalize(this);
    }

    private class WmiMonitorData
    {
        public string InstancePath { get; set; } = "";
        public string FriendlyName { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string Manufacturer { get; set; } = "";
    }
}
