using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using GammaController.Interop;

namespace GammaController.Helpers;

public static class WindowEffects
{
    public static void EnableAcrylic(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        // Enable dark mode for the window
        int darkMode = 1;
        NativeMethods.DwmSetWindowAttribute(
            hwnd,
            DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref darkMode,
            sizeof(int));

        // Set rounded corners (Windows 11)
        int cornerPreference = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(
            hwnd,
            DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
            ref cornerPreference,
            sizeof(int));

        // Try the new Windows 11 backdrop API first
        int backdropType = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW; // Acrylic
        int result = NativeMethods.DwmSetWindowAttribute(
            hwnd,
            DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropType,
            sizeof(int));

        if (result != 0)
        {
            // Fallback to legacy acrylic using SetWindowCompositionAttribute
            EnableAcrylicLegacy(hwnd);
        }

        // Extend frame into client area - required for blur to work
        var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);

        // Set the window background - dark tint but not too dark
        window.Background = new SolidColorBrush(Color.FromArgb(160, 25, 25, 25));
    }

    private static void EnableAcrylicLegacy(IntPtr hwnd)
    {
        // Use ACCENT_ENABLE_ACRYLICBLURBEHIND for Windows 10 1803+
        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 2, // ACCENT_FLAG_DRAW_ALL_BORDERS
            GradientColor = 0xA0191919 // AABBGGRR format - dark tint (0xA0 = 160 alpha, 0x19 = 25 RGB)
        };

        var accentSize = Marshal.SizeOf(accent);
        var accentPtr = Marshal.AllocHGlobal(accentSize);
        
        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                Data = accentPtr,
                SizeOfData = accentSize
            };

            NativeMethods.SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }

    public static void MakeToolWindow(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        // Set as tool window (doesn't appear in taskbar/alt-tab)
        int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
    }
}
