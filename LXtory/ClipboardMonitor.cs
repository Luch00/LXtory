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
            private HwndSource source;
            public ClipboardMonitorWindow()
            {
                this.Closing += ClipboardMonitorWindow_Closing;
                var helper = new WindowInteropHelper(this).EnsureHandle();
                source = HwndSource.FromHwnd(helper);
                source.AddHook(new HwndSourceHook(WndProc));
                NativeMethods.AddClipboardFormatListener(source.Handle);
            }

            private void ClipboardMonitorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
            {
                DisableMonitor();
                source.Dispose();
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
