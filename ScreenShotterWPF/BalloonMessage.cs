using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Drawing;

namespace ScreenShotterWPF
{
    internal static class BalloonMessage
    {
        private static TaskbarIcon notification;
        public static TaskbarIcon Notification
        {
            private get { return notification; }
            set
            {
                notification = value;
                notification.TrayBalloonTipClicked += Notification_TrayBalloonTipClicked;
            }
        }
        private static Icon defaulticon = new Icon(App.GetResourceStream(new Uri($"pack://application:,,,/Resources/Hoshimemo5.ico", UriKind.Absolute)).Stream, 48, 48);
        public static EventHandler ClipboardNotificationClicked;
        private static bool isClipboard = false;

        private static void OnClipboardNotificationClicked(EventArgs e)
        {
            ClipboardNotificationClicked?.Invoke(null, e);
        }

        public static void ShowMessage(string s, BalloonIcon icon)
        {
            isClipboard = false;
            Notification?.ShowBalloonTip("LXtory", s, icon);
        }

        public static void ShowMessage(string s)
        {
            isClipboard = false;
            Notification?.ShowBalloonTip("LXtory", s, defaulticon, true);
        }

        public static void ClipboardNotification()
        {
            isClipboard = true;
            Notification?.ShowBalloonTip("LXtory", $"Image on clipboard{Environment.NewLine}Click to upload", defaulticon, true);
        }

        private static void Notification_TrayBalloonTipClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isClipboard)
            {
                OnClipboardNotificationClicked(null); 
            }
        }
    }
}
