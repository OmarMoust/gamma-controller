using System.IO;
using System.Text.Json;
using GammaController.Models;
using Microsoft.Win32;

namespace GammaController.Services;

public class SettingsService
{
    private const string AppName = "GammaController";
    private const string SettingsFileName = "settings.json";
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    
    private readonly string _settingsPath;
    private AppSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings Settings => _settings;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppName);
        
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, SettingsFileName);
        _settings = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
        
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public double GetMonitorGamma(string serialNumber)
    {
        if (_settings.MonitorSettings.TryGetValue(serialNumber, out var settings))
            return settings.Gamma;
        return 1.0;
    }

    public void SetMonitorGamma(string serialNumber, string friendlyName, double gamma)
    {
        if (!_settings.MonitorSettings.ContainsKey(serialNumber))
        {
            _settings.MonitorSettings[serialNumber] = new MonitorSettings();
        }
        
        _settings.MonitorSettings[serialNumber].FriendlyName = friendlyName;
        _settings.MonitorSettings[serialNumber].Gamma = gamma;
        Save();
    }

    public bool IsMonitorKnown(string serialNumber)
    {
        return _settings.MonitorSettings.ContainsKey(serialNumber);
    }

    public bool RunAtStartup
    {
        get => _settings.RunAtStartup;
        set
        {
            _settings.RunAtStartup = value;
            UpdateStartupRegistry(value);
            Save();
        }
    }

    public bool ApplyOnConnect
    {
        get => _settings.ApplyOnConnect;
        set
        {
            _settings.ApplyOnConnect = value;
            Save();
        }
    }

    private void UpdateStartupRegistry(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update startup registry: {ex.Message}");
        }
    }

    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }
}

