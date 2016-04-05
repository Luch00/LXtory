using System;
using System.Windows;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using ScreenShotterWPF.Notifications;
using System.Windows.Input;
using Prism.Commands;

namespace ScreenShotterWPF.ViewModels
{
    class OverlayViewModel : BindableBase, IInteractionRequestAware
    {
        private OverlayNotification notification;

        public Action FinishInteraction { get; set; }

        public ICommand EscapeCommand { get; private set; }
        
        private Point start;
        private double rectHeight;
        private double rectWidth;
        private Thickness rectMargin;
        private Thickness textMargin;
        private string text;

        public string Text
        {
            get { return text; }
            set { text = value; OnPropertyChanged("Text"); }
        }

        public double RectHeight
        {
            get { return rectHeight; }
            set { rectHeight = value; OnPropertyChanged("RectHeight"); }
        }

        public double RectWidth
        {
            get { return rectWidth; }
            set { rectWidth = value; OnPropertyChanged("RectWidth"); }
        }

        public Thickness RectMargin
        {
            get { return rectMargin; }
            set { rectMargin = value; OnPropertyChanged("RectMargin"); }
        }

        public Thickness TextMargin
        {
            get { return textMargin; }
            set { textMargin = value; OnPropertyChanged("TextMargin"); }
        }

        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is OverlayNotification)
                {
                    this.notification = value as OverlayNotification;
                    RectWidth = 0;
                    RectHeight = 0;
                    Text = "";
                    this.OnPropertyChanged(() => this.Notification);
                }
            }
        }

        public OverlayViewModel()
        {
            EscapeCommand = new DelegateCommand(Close);
        }

        private void Close()
        {
            if (this.notification != null)
            {
                this.notification.Confirmed = false;
            }
            this.FinishInteraction();
        }

        public void PaintSurface_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                start = e.GetPosition(null);
                RectHeight = 0;
                RectWidth = 0;
                RectMargin = new Thickness(start.X, start.Y,0,0);
            }
        }

        public void PaintSurface_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            var pos = e.GetPosition(null);

            var x = Math.Min(pos.X, start.X);
            var y = Math.Min(pos.Y, start.Y);

            var w = Math.Max(pos.X, start.X) - x;
            var h = Math.Max(pos.Y, start.Y) - y;

            RectHeight = h;
            RectWidth = w;
            RectMargin = new Thickness(x, y, 0, 0);

            if (w > 1)
            {   
                Text = $"H: {h}\nW: {w}";
                double xpos = x + w - 50;
                double ypos = y + h - 35;
                TextMargin = new Thickness(xpos, ypos, 0, 0);
            }
        }

        public void PaintSurface_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            if (this.notification != null)
            {
                this.notification.Rect = new Rect(RectMargin.Left, RectMargin.Top, RectWidth, RectHeight);
                this.notification.Confirmed = true;
            }
            this.FinishInteraction();
        }

        public void PaintSurface_MouseRightButtonUp(object sender, MouseEventArgs e)
        {
            Close();
        }
    }
}
