using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenShotterWPF
{
    internal static class NativeMethods
    {
        // p/invokes and stuff
        [DllImport("user32.dll")]
        internal static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        //[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //private static extern IntPtr GetModuleHandle(string lpModuleName);

        //[DllImport("user32.dll")]
        //internal static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        //[DllImport("user32.dll")]
        //internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        //[DllImport("user32.dll")]
        //internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        internal static extern IntPtr WindowFromPoint(int xPoint, int yPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("shell32.dll")]
        internal static extern int SHQueryUserNotificationState(out USERNOTIFICATIONSTATE pquns);

        // Clipboard pinvokes
        //[DllImport("user32.dll", SetLastError = true)]
        //internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        //[DllImport("user32.dll", SetLastError = true)]
        //internal static extern bool CloseClipboard();

        //[DllImport("user32.dll", SetLastError = true)]
        //internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

        //[DllImport("user32.dll", SetLastError = true)]
        //internal static extern bool EmptyClipboard();

        //[DllImport("user32.dll")]
        //internal static extern IntPtr GetOpenClipboardWindow();

        //[DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //internal static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("User32.dll")]
        internal static extern bool RegisterHotKey(
            [In] IntPtr hWnd,
            [In] int id,
            [In] uint fsModifiers,
            [In] uint vk);

        [DllImport("User32.dll")]
        internal static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        //internal delegate void WinEventDelegate(IntPtr hWinventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [StructLayout(LayoutKind.Sequential)]
        internal struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINTAPI
        {
            public int x;
            public int y;
        }

        // Rectangle for window and area capture
        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;
        }

        // Point for cursorpos
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        //[StructLayout(LayoutKind.Sequential)]
        //internal struct WINDOWPLACEMENT
        //{
        //    public int length;
        //    public int flags;
        //    public int showCmd;
        //    public POINTAPI ptMinPosition;
        //    public POINTAPI ptMaxPosition;
        //    public RECT rcNormalPosition;
        //}

        internal enum USERNOTIFICATIONSTATE
        {
            QUNS_NOT_PRESENT = 1,
            QUNS_BUSY = 2,
            QUNS_RUNNING_D3D_FULL_SCREEN = 3,
            QUNS_PRESENTATION_MODE = 4,
            QUNS_ACCEPTS_NOTIFICATIONS = 5,
            QUNS_QUIET_TIME = 6
        };

        internal const Int32 CURSOR_SHOWING = 0x0001;
        internal const Int32 DI_NORMAL = 0x0003;
    }
}
