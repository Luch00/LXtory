using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Drawing;

namespace ScreenShotterWPF
{
    internal static class BalloonMessage
    {
        public static TaskbarIcon Notification { private get; set; }
        private static Icon defaulticon = new Icon(App.GetResourceStream(new Uri($"pack://application:,,,/Resources/Hoshimemo5.ico", UriKind.Absolute)).Stream, 48, 48);
        
        public static void ShowMessage(string s, BalloonIcon icon)
        {
            Notification?.ShowBalloonTip("LXtory", s, icon);
        }

        public static void ShowMessage(string s)
        {
            Notification?.ShowBalloonTip("LXtory", s, defaulticon, true);
        }
    }
}
