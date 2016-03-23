using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using ScreenShotterWPF.Notifications;

namespace ScreenShotterWPF.ViewModels
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
                    // To keep the code simple, this is the only property where we are raising the PropertyChanged event,
                    // as it's required to update the bindings when this property is populated.
                    // Usually you would want to raise this event for other properties too.
                    this.notification = value as GifProgressNotification;
                    this.OnPropertyChanged(() => this.Notification);
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
            string name = await this.notification.Gif.EncodeGif2(this.notification.Frames, this.notification);
            if (name != String.Empty)
            {
                this.notification.Name = name;
                this.notification.Confirmed = true;
                this.FinishInteraction();
            }
            this.notification.Confirmed = false;
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
