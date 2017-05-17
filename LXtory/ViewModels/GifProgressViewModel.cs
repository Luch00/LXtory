using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using LXtory.Notifications;

namespace LXtory.ViewModels
{
    class GifProgressViewModel : BindableBase, IInteractionRequestAware
    {
        private GifProgressNotification notification;

        public Action FinishInteraction { get; set; }

        public ICommand CancelCommand { get; private set; }

        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is GifProgressNotification)
                {
                    this.notification = value as GifProgressNotification;
                    //OnPropertyChanged(() => Notification);
                    RaisePropertyChanged();
                    StartEncode();
                }
            }
        }

        public GifProgressViewModel()
        {
            this.CancelCommand = new DelegateCommand(Cancel);
        }

        private async void StartEncode()
        {
            string name = await this.notification.Gif.EncodeGif(this.notification);
            if (name != String.Empty)
            {
                this.notification.Name = name;
                this.notification.Confirmed = true;
            }
            else
            {
                this.notification.Confirmed = false;
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
    }
}
