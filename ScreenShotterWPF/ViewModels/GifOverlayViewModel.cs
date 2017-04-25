using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using ScreenShotterWPF.Notifications;
using System;
using System.Windows;
using System.Windows.Input;

namespace ScreenShotterWPF.ViewModels
{
    class GifOverlayViewModel : BindableBase, IInteractionRequestAware
    {
        private GifOverlayNotification notification;

        public Action FinishInteraction { get; set; }

        private ICommand cancelCaptureCommand;
        private ICommand startCaptureCommand;
        private ICommand loadCacheCommand;

        private double maxWidth;
        private double maxHeight;
        private int windowWidth;
        private int windowHeight;
        private int gifDuration;
        private int gifFramerate;

        public GifOverlayViewModel()
        {
            windowHeight = 300;
            windowWidth = 300;
            SetMaxWidthHeight();
            startCaptureCommand = new DelegateCommand(Start, CanStart);
            cancelCaptureCommand = new DelegateCommand(Cancel);
            loadCacheCommand = new DelegateCommand(LoadCache);
        }

        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is GifOverlayNotification)
                {
                    this.notification = value as GifOverlayNotification;
                    //this.OnPropertyChanged(() => this.Notification);
                    this.RaisePropertyChanged(nameof(this.Notification));
                    GifDuration = Properties.Settings.Default.gifDuration;
                    GifFramerate = Properties.Settings.Default.gifFrameRate;
                }
            }
        }

        private void SetMaxWidthHeight()
        {
            maxHeight = SystemParameters.VirtualScreenHeight;
            maxWidth = SystemParameters.VirtualScreenWidth;
        }

        private void Start()
        {
            if (notification != null)
            {
                notification.GifDuration = this.GifDuration;
                notification.GifFramerate = this.GifFramerate;
                notification.Confirmed = true;
            }
            this.FinishInteraction();
        }

        private void LoadCache()
        {
            if (notification != null)
            {
                notification.GifDuration = this.GifDuration;
                notification.GifFramerate = this.GifFramerate;
                notification.LoadCache = true;
                notification.Confirmed = true;
            }
            this.FinishInteraction();
        }

        private void Cancel()
        {
            if (notification != null)
            {
                notification.Confirmed = false;
            }
            this.FinishInteraction();
        }

        private bool CanStart()
        {
            return (WindowHeight > 0 && WindowWidth > 0) ? true : false;
        }

        public ICommand StartCaptureCommand
        {
            get { return startCaptureCommand; }
            set { startCaptureCommand = value; }
        }

        public ICommand CancelCaptureCommand
        {
            get { return cancelCaptureCommand; }
            set { cancelCaptureCommand = value; }
        }

        public ICommand LoadCacheCommand
        {
            get { return loadCacheCommand; }
            set { loadCacheCommand = value; }
        }

        public int GifDuration
        {
            get { return gifDuration; }
            set { SetProperty(ref gifDuration, value); }
        }

        public int GifFramerate
        {
            get { return gifFramerate; }
            set { SetProperty(ref gifFramerate, value); }
        }

        public double MaxWidth
        {
            get { return maxWidth; }
            set { SetProperty(ref maxWidth, value); }
        }

        public double MaxHeight
        {
            get { return maxHeight; }
            set { SetProperty(ref maxHeight, value); }
        }

        public int WindowWidth
        {
            get { return windowWidth; }
            set
            {
                SetProperty(ref windowWidth, value);
                notification.WindowWidth = value;
            }
        }

        public int WindowHeight
        {
            get { return windowHeight; }
            set
            {
                SetProperty(ref windowHeight, value);
                notification.WindowHeight = value;
            }
        }

        public void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize != e.PreviousSize)
            {
                WindowHeight = (int)e.NewSize.Height;
                WindowWidth = (int)e.NewSize.Width;
            }
        }
    }
}
