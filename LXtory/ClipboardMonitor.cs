using System;
using System.Windows;
using System.Windows.Interop;

namespace LXtory
{
    internal static class ClipboardMonitor
    {
        public static EventHandler ClipboardEvent;

        private static ClipboardMonitorWindow window;

        private static void OnClipboardEvent(EventArgs e)
        {
            ClipboardEvent?.Invoke(null, e);
        }

        public static void EnableMonitor()
        {
            if (window == null)
            {
                window = new ClipboardMonitorWindow();
            }
        }

        public static void DisableMonitor()
        {
            if (window != null)
            {
                NativeMethods.RemoveClipboardFormatListener(new WindowInteropHelper(window).Handle);
                window = null;
            }
        }

        class ClipboardMonitorWindow : Window
        {
            private const int WM_CLIPBOARDUPDATE = 0x031D;
            
            public ClipboardMonitorWindow()
            {
                var helper = new WindowInteropHelper(this).EnsureHandle();
                //using (HwndSource source = HwndSource.FromHwnd(helper))
                //{
                HwndSource source = HwndSource.FromHwnd(helper);
                    source.AddHook(new HwndSourceHook(WndProc));
                    NativeMethods.AddClipboardFormatListener(source.Handle);
                //}
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
