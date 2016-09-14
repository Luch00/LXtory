using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hardcodet.Wpf.TaskbarNotification;

namespace ScreenShotterWPF
{
    internal static class BalloonMessage
    {
        public static TaskbarIcon Notification { private get; set; }

        public static void ShowMessage(string s, BalloonIcon icon)
        {
            Notification?.ShowBalloonTip("LXtory", s, icon);
        }
    }
}
