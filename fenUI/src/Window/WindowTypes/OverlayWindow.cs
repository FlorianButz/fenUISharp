using System.Runtime.InteropServices;
using FenUISharp.Mathematics;
using FenUISharp.Native;
using Microsoft.Win32;
using SkiaSharp;

namespace FenUISharp
{

    public class OverlayWindow : TransparentWindow
    {
        private int _activeDisplay = 0;
        public int ActiveDisplayIndex { get => _activeDisplay; set { _activeDisplay = value; UpdateWindowMetrics(_activeDisplay); } }

        public OverlayWindow(
            string title, string className, RenderContextType type, int monitorIndex = 0) :
            base(title, className, type, null, null)
        {
            if (type == RenderContextType.DirectX) throw new ArgumentException("DirectX is currently not supported with transparent windows.");

            AllowResizing = false;
            UpdateWindowMetrics(monitorIndex);
        }

        public void UpdateWindowMetrics(int activeMonitorDisplay = 0)
        {
            int x, y, width, height;

            if (activeMonitorDisplay == 0)
            {
                // Use primary monitor metrics from system metrics
                width = GetSystemMetrics(0);  // SM_CXSCREEN
                height = GetSystemMetrics(1); // SM_CYSCREEN
                x = 0;
                y = 0;
            }
            else
            {
                var monitorRect = GetMonitorRect(activeMonitorDisplay);

                x = monitorRect.left;
                y = monitorRect.top;
                width = monitorRect.right - monitorRect.left;
                height = monitorRect.bottom - monitorRect.top;
            }

            WindowSize = new Vector2(width, height);
            WindowPosition = new Vector2(x, y);
            SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, (uint)SetWindowPosFlags.SWP_NOZORDER | (uint)SetWindowPosFlags.SWP_NOACTIVATE);

            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);

            if (EnumDisplayDevices(null, (uint)_activeDisplay, ref d, 0))
            {
                DEVMODE vDevMode = new DEVMODE();
                vDevMode.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE));

                if (EnumDisplaySettings(d.DeviceName, ENUM_CURRENT_SETTINGS, ref vDevMode))
                {
                    // Console.WriteLine($"Monitor {_activeDisplay} Refresh Rate: {vDevMode.dmDisplayFrequency} Hz");
                    RefreshRate = (int)vDevMode.dmDisplayFrequency;
                }
            }
        }

        protected override IntPtr CreateWin32Window(WNDCLASSEX wndClass, Vector2? size, Vector2? position)
        {
            WindowSize = new Vector2(GetSystemMetrics(0), GetSystemMetrics(1));

            return base.CreateWin32Window(wndClass, WindowSize, position);
        }

        const int ENUM_CURRENT_SETTINGS = -1;

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency; // <--- THIS is your refresh rate
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }
}