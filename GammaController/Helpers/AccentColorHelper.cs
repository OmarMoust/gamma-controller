using Microsoft.Win32;
using System.Windows.Media;

namespace GammaController.Helpers;

public static class AccentColorHelper
{
    public static Color GetAccentColor()
    {
        try
        {
            // Read from Windows personalization registry (most reliable)
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", false);
            
            if (key?.GetValue("AccentColorMenu") is int accentColor)
            {
                // Registry stores as ABGR (Alpha, Blue, Green, Red)
                byte r = (byte)(accentColor & 0xFF);
                byte g = (byte)((accentColor >> 8) & 0xFF);
                byte b = (byte)((accentColor >> 16) & 0xFF);
                
                // Make 10% brighter for better contrast
                r = (byte)Math.Min(255, r + 25);
                g = (byte)Math.Min(255, g + 25);
                b = (byte)Math.Min(255, b + 25);
                
                return Color.FromArgb(255, r, g, b);
            }
        }
        catch { }

        try
        {
            // Fallback: try DWM accent
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM", false);
            
            if (key?.GetValue("AccentColor") is int accentColor)
            {
                // Also stored as ABGR
                return Color.FromArgb(
                    255,
                    (byte)(accentColor & 0xFF),
                    (byte)((accentColor >> 8) & 0xFF),
                    (byte)((accentColor >> 16) & 0xFF));
            }
        }
        catch { }

        // Default Windows blue accent
        return Color.FromRgb(0, 120, 215);
    }

    public static Brush GetAccentBrush()
    {
        return new SolidColorBrush(GetAccentColor());
    }
}
