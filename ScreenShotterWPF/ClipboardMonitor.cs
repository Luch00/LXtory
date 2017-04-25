using System;
using System.Windows;
using System.Windows.Interop;

namespace ScreenShotterWPF
{
    static class ClipboardMonitor
    {
        public static EventHandler ClipboardEvent;

        private static ClipboardMonitorWindow window = new ClipboardMonitorWindow();

        private static void OnClipboardEvent(EventArgs e)
        {
            ClipboardEvent?.Invoke(null, e);
        }

        class ClipboardMonitorWindow : Window
        {
            private const int WM_CLIPBOARDUPDATE = 0x031D;
            public ClipboardMonitorWindow()
            {
                var helper = new WindowInteropHelper(this).EnsureHandle();
                HwndSource source = HwndSource.FromHwnd(helper);
                source.AddHook(new HwndSourceHook(WndProc));
                NativeMethods.AddClipboardFormatListener(source.Handle);
            }
            private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == WM_CLIPBOARDUPDATE)
                {
                    OnClipboardEvent(null);
                }
                return IntPtr.Zero;
            }
        }
    }
}
