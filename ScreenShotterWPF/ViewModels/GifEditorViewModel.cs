using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using ScreenShotterWPF.Notifications;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenShotterWPF.ViewModels
{
    internal class GifEditorViewModel : BindableBase, IInteractionRequestAware
    {
        private GifEditorNotification notification;

        private ImageSource previewImage;

        public Action FinishInteraction { get; set; }
        
        private int selectedIndex;
        private ICommand checkBeginningCommand;
        private ICommand checkEndCommand;
        private ICommand uncheckBeginningCommand;
        private ICommand uncheckEndCommand;
        private ICommand encodeCommand;
        public ICommand CancelCommand { get; private set; }

        public GifEditorViewModel()
        {
            checkBeginningCommand = new DelegateCommand(CheckFromBeginning, CanCheckUncheck);
            checkEndCommand = new DelegateCommand(CheckFromEnd, CanCheckUncheck);
            uncheckBeginningCommand = new DelegateCommand(UncheckFromBeginning, CanCheckUncheck);
            uncheckEndCommand = new DelegateCommand(UncheckFromEnd, CanCheckUncheck);
            encodeCommand = new DelegateCommand(StartEncode);
            CancelCommand = new DelegateCommand(Cancel);
            selectedIndex = 0;
        }

        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is GifEditorNotification)
                {
                    this.notification = value as GifEditorNotification;
                    notification.Gif.LoadThumbnails();
                    SelectedIndex = 0;
                    this.OnPropertyChanged(() => this.Notification);
                }
            }
        }

        public ICommand CheckBeginningCommand
        {
            get { return checkBeginningCommand; }
            set { checkBeginningCommand = value; }
        }
        public ICommand CheckEndCommand
        {
            get { return checkEndCommand; }
            set { checkEndCommand = value; }
        }
        public ICommand UncheckBeginningCommand
        {
            get { return uncheckBeginningCommand; }
            set { uncheckBeginningCommand = value; }
        }
        public ICommand UncheckEndCommand
        {
            get { return uncheckEndCommand; }
            set { uncheckEndCommand = value; }
        }
        public ICommand EncodeCommand
        {
            get { return encodeCommand; }
            set { encodeCommand = value; }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; SetPreviewImage(); OnPropertyChanged("SelectedIndex"); }
        }

        public ImageSource PreviewImage
        {
            get { return previewImage; }
            set { previewImage = value; OnPropertyChanged("PreviewImage"); }
        }

        private void SetPreviewImage()
        {
            if (previewImage != null)
            {
                previewImage = null;
            }
            PreviewImage = notification.Gif.Frames[selectedIndex].Image;
        }

        private void Cancel()
        {
            if (notification != null)
            {
                notification.Confirmed = false;
            }
            this.FinishInteraction();
        }

        private void StartEncode()
        {
            if (notification != null)
            {
                notification.Confirmed = true;
            }
            this.FinishInteraction();
        }

        private void CheckFromBeginning()
        {   
            CheckUncheckBoxes(true, false);
        }

        private void UncheckFromBeginning()
        {
            CheckUncheckBoxes(false, false);
        }

        private void CheckFromEnd()
        {
            CheckUncheckBoxes(true, true);
        }

        private void UncheckFromEnd()
        {
            CheckUncheckBoxes(false, true);
        }

        private void CheckUncheckBoxes(bool check, bool fromEnd)
        {
            int selected = SelectedIndex;
            if (fromEnd)
            {
                for (int i = notification.Gif.Frames.Count - 1; i >= selected; i--)
                {
                    notification.Gif.Frames[i].Selected = check;
                }
            }
            else
            {
                for (int i = 0; i <= selected; i++)
                {
                    notification.Gif.Frames[i].Selected = check;
                }
            }
        }

        private bool CanCheckUncheck()
        {
            return (SelectedIndex > -1) ? true : false;
        }
    }
}
