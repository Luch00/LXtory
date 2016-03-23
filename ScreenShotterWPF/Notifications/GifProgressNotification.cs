using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

namespace ScreenShotterWPF.Notifications
{
    class GifProgressNotification : BindableBase, IConfirmation
    {
        private int progress;

        public bool Confirmed { get; set; }

        public object Content { get; set; }

        public string Title { get; set; }

        public Gif Gif { get; set; }
        public List<string> Frames { get; set; }
        public bool Cancelled { get; set; }
        public string Name { get; set; }

        public int Progress
        {
            get { return progress; }
            set { progress = value; OnPropertyChanged("Progress"); }
        }

        public GifProgressNotification()
        {
            Frames = new List<string>();
            Cancelled = false;
        }
    }
}
