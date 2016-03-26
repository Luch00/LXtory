using System.ComponentModel;
using System.Windows.Input;

namespace ScreenShotterWPF
{
    internal class EncodingProgressViewModel : INotifyPropertyChanged
    {
        private int progressValue;
        private bool cancelRequested;
        private ICommand cancelCommand;        

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand CancelCommand
        {
            get { return cancelCommand; }
            set { cancelCommand = value; }
        }

        public EncodingProgressViewModel()
        {
            progressValue = 0;
            cancelRequested = false;
            //cancelCommand = new RelayCommand(SetCancel, param => true);
        }

        public int ProgressValue
        {
            get { return progressValue; }
            set { progressValue = value; RaisePropertyChanged("ProgressValue"); }
        }

        public bool CancelRequested
        {
            get { return cancelRequested; }
        }

        private void SetCancel(object param)
        {
            cancelRequested = true;
        }
    }
}
