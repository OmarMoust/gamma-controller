# Gamma Controller

A lightweight Windows system tray application for adjusting monitor gamma/brightness with per-monitor settings and automatic restoration.

![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?logo=windows11)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- **Per-Monitor Gamma Control** - Adjust gamma independently for each connected display
- **Windows 11 Fluent UI** - Modern acrylic backdrop with system accent color integration, fits right in with the Windows 11 taskbar controls
- **Persistent Settings** - Gamma preferences saved per monitor serial number
- **Auto-Apply on Connect** - Automatically restores gamma when monitors are reconnected
- **Hotplug Detection** - Detects monitor connect/disconnect events in real-time
- **System Tray Integration** - Minimal footprint, always accessible from the notification area
- **Single Instance** - Only one instance runs at a time; clicking the tray icon toggles visibility
- **Run at Startup** - Optional Windows startup integration

## Installation

### Option 1: Installer (Recommended)
Download the latest `GammaController-Setup-x.x.x.exe` from the [Releases](../../releases) page.

### Option 2: Build from Source

**Prerequisites:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

```bash
# Clone the repository
git clone https://github.com/OmarMoust/gamma-controller.git
cd gamma-controller

# Build and run
dotnet build GammaController
dotnet run --project GammaController
```

**Create a self-contained executable:**
```bash
dotnet publish GammaController -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

## Usage

1. Open the system tray
2. Click the Gamma Controller sun icon to show the adjustment popup
3. Use the slider to adjust gamma (0-100 scale)
4. For multi-monitor setups, use the segmented buttons to switch between displays
5. Right-click the tray icon for additional options:
   - Run at startup
   - Apply gamma on connect
   - Reset all to default
   - Exit

## Technical Details

### Architecture

```
GammaController/
├── Converters/          # WPF value converters
├── Helpers/             # Window effects & accent color
├── Interop/             # Windows API P/Invoke declarations
├── Models/              # Data models (settings, monitor info)
├── Services/            # Core business logic
│   ├── GammaService     # GDI gamma ramp manipulation
│   ├── SettingsService  # JSON persistence & registry
│   ├── MonitorWatcher   # WMI device change events
│   └── SingleInstance   # Named pipe IPC
└── Themes/              # Fluent Design resources
```

### Technologies Used

- **WPF** - Windows Presentation Foundation for UI
- **Windows GDI** - `SetDeviceGammaRamp` / `GetDeviceGammaRamp` for gamma control
- **WMI** - `WmiMonitorID` for monitor serial numbers, `Win32_DeviceChangeEvent` for hotplug
- **DWM** - Desktop Window Manager for acrylic backdrop and rounded corners
- **Named Pipes** - Inter-process communication for single instance enforcement

## Building the Installer

The installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php):

```bash
# From the installer directory
iscc GammaController.iss
```

Output: `dist/GammaController-Setup-1.0.0.exe`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) - System tray icon library
- Windows 11 Fluent Design System for UI inspiration
