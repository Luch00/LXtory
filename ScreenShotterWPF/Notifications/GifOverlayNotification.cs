using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenShotterWPF.Notifications
{
    class GifOverlayNotification : BindableBase, IConfirmation
    {
        private int windowHeight;
        private int windowWidth;

        public int GifFramerate { get; set; }
        public int GifDuration { get; set; }
        public int WindowTop { get; set; }
        public int WindowLeft { get; set; }
        
        public int WindowHeight
        {
            get { return windowHeight; }
            set
            {
                windowHeight = value;
                OnPropertyChanged("WindowHeight");
            }
        }
        public int WindowWidth
        {
            get { return windowWidth; }
            set
            {
                windowWidth = value;
                OnPropertyChanged("WindowWidth");
            }
        }

        public bool Confirmed
        {
            get; set;
        }

        public string Title
        {
            get; set;
        }

        public object Content
        {
            get; set;
        }

        public GifOverlayNotification()
        {
            WindowHeight = 300;
            WindowWidth = 300;
            WindowTop = (int)SystemParameters.PrimaryScreenHeight / 2 - 150;
            WindowLeft = (int)SystemParameters.PrimaryScreenWidth / 2 - 150;
        }
    }
}
