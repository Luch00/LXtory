using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ScreenShotterWPF
{
    class GifOverlayViewModel : INotifyPropertyChanged
    {
        private ICommand cancelCaptureCommand;
        private ICommand startCaptureCommand;

        private double maxWidth;
        private double maxHeight;
        private int windowWidth;
        private int windowHeight;
        private int gifDuration;
        private int gifFramerate;

        public event PropertyChangedEventHandler PropertyChanged;

        public GifOverlayViewModel()
        {
            windowHeight = 300;
            windowWidth = 300;
            gifDuration = Properties.Settings.Default.gifDuration;
            gifFramerate = Properties.Settings.Default.gifFrameRate;
            SetMaxWidthHeight();
            startCaptureCommand = new RelayCommand(Start, CanStart);
            cancelCaptureCommand = new RelayCommand(Cancel, param => true);
        }

        private void SetMaxWidthHeight()
        {
            maxHeight = SystemParameters.VirtualScreenHeight;
            maxWidth = SystemParameters.VirtualScreenWidth;
        }

        private void Start(object param)
        {
            Window w = param as Window;
            w.DialogResult = true;
        }

        private void Cancel(object param)
        {
            Window w = param as Window;
            w.DialogResult = false;
        }

        private bool CanStart(object param)
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

        public int GifDuration
        {
            get { return gifDuration; }
            set { gifDuration = value; RaisePropertyChanged("GifDuration"); }
        }

        public int GifFramerate
        {
            get { return gifFramerate; }
            set { gifFramerate = value; RaisePropertyChanged("GifFramerate"); }
        }

        public double MaxWidth
        {
            get { return maxWidth; }
            set { maxWidth = value; RaisePropertyChanged("MaxWidth"); }
        }

        public double MaxHeight
        {
            get { return maxHeight; }
            set { maxHeight = value; RaisePropertyChanged("MaxHeight"); }
        }

        public int WindowWidth
        {
            get { return windowWidth; }
            set { windowWidth = value; RaisePropertyChanged("WindowWidth"); }
        }

        public int WindowHeight
        {
            get { return windowHeight; }
            set { windowHeight = value; RaisePropertyChanged("WindowHeight"); }
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
