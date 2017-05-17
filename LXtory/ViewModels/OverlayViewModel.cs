using System;
using System.Windows;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using LXtory.Notifications;
using System.Windows.Input;
using Prism.Commands;

namespace LXtory.ViewModels
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
        private double dpix;
        private double dpiy;

        public string Text
        {
            get { return text; }
            set { SetProperty(ref text, value); }
            //set { text = value; OnPropertyChanged("Text"); }
        }

        public double RectHeight
        {
            get { return rectHeight; }
            set { SetProperty(ref rectHeight, value); }
            //set { rectHeight = value; OnPropertyChanged("RectHeight"); }
        }

        public double RectWidth
        {
            get { return rectWidth; }
            set { SetProperty(ref rectWidth, value); }
            //set { rectWidth = value; OnPropertyChanged("RectWidth"); }
        }

        public Thickness RectMargin
        {
            get { return rectMargin; }
            set { SetProperty(ref rectMargin, value); }
            //set { rectMargin = value; OnPropertyChanged("RectMargin"); }
        }

        public Thickness TextMargin
        {
            get { return textMargin; }
            set { SetProperty(ref textMargin, value); }
            //set { textMargin = value; OnPropertyChanged("TextMargin"); }
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
                    GetDPIMultiplier();
                    //this.OnPropertyChanged(() => this.Notification);
                    this.RaisePropertyChanged(nameof(this.Notification));
                }
            }
        }

        public OverlayViewModel()
        {
            EscapeCommand = new DelegateCommand(Close);
        }

        private void GetDPIMultiplier()
        {
            System.Windows.Media.Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
            this.dpix = m.M11;
            this.dpiy = m.M22;
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
                Text = $"H: {Math.Floor(h*dpiy)}\nW: {Math.Floor(w*dpix)}";
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
