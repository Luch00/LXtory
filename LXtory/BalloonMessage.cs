using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Timers;
using System.Windows;

namespace LXtory
{
    internal static class BalloonMessage
    {
        private static readonly Dictionary<string, BitmapImage> trayicons = new Dictionary<string, BitmapImage>();
        private static readonly Timer timer = new Timer();
        private static TaskbarIcon notification;
        public static TaskbarIcon Notification
        {
            private get { return notification; }
            set
            {
                LoadIcons();
                notification = value;
                timer.Interval = 5000;
                timer.Elapsed += timerTick_DelayIconChange;
                SetIcon("Default");
                notification.TrayBalloonTipClicked += Notification_TrayBalloonTipClicked;
            }
        }

        private static void timerTick_DelayIconChange(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            SetIcon("Default");
        }

        private static readonly Icon defaulticon = new Icon(App.GetResourceStream(new Uri($"pack://application:,,,/Resources/Hoshimemo5.ico", UriKind.Absolute)).Stream, 48, 48);
        public static EventHandler ClipboardNotificationClicked;
        private static bool isClipboard = false;

        //public enum TrayIcon
        //{
        //    Default,
        //    Finished,
        //    Error,
        //    Refresh,
        //    P10,
        //    P20,
        //    P30,
        //    P40,
        //    P50,
        //    P60,
        //    P70,
        //    P80,
        //    P90
        //}

        public static void SetIcon(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                timer.Stop();
                Application.Current.Dispatcher.Invoke(() => { Notification.IconSource = trayicons[s]; });
                if (s == "F")
                    timer.Start();
            }
            else
            {
                Notification.IconSource = trayicons["Default"];
            }
        }

        private static void LoadIcons()
        {
            string[] ico = { "Default", "F", "E", "R", "10", "20", "30", "40", "50", "60", "70", "80", "90" };
            foreach (var i in ico)
            {
                var bitmapImage = new BitmapImage(new Uri($"pack://application:,,,/Resources/{i}.ico", UriKind.Absolute));
                bitmapImage.Freeze();
                trayicons.Add(i, bitmapImage);
            }
        }

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

        private static void Notification_TrayBalloonTipClicked(object sender, RoutedEventArgs e)
        {
            if (isClipboard)
            {
                OnClipboardNotificationClicked(null); 
            }
        }
    }
}
