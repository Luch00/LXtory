using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using ScreenShotterWPF.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ScreenShotterWPF.ViewModels
{
    internal class GifEditorViewModel : BindableBase, IInteractionRequestAware
    {
        private GifEditorNotification notification;

        public Action FinishInteraction { get; set; }

        private ObservableCollection<GifFrame> items;
        private int selectedIndex;
        private ICommand checkBeginningCommand;
        private ICommand checkEndCommand;
        private ICommand uncheckBeginningCommand;
        private ICommand uncheckEndCommand;
        private ICommand encodeCommand;
        public ICommand CancelCommand { get; private set; }

        public GifEditorViewModel()
        {
            items = new ObservableCollection<GifFrame>();
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
                    PopulateListBox(notification.Frames);
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

        public ObservableCollection<GifFrame> Items
        {
            get { return items; }
            set { items = value; }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; OnPropertyChanged("SelectedIndex"); }
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
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Selected)
                    {
                        notification.SelectedIndexes.Add(i);
                    }
                }
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
                for (int i = items.Count - 1; i >= selected; i--)
                {
                    items[i].Selected = check;
                }
            }
            else
            {
                for (int i = 0; i <= selected; i++)
                {
                    items[i].Selected = check;
                }
            }
        }

        private bool CanCheckUncheck()
        {
            return (SelectedIndex > -1) ? true : false;
        }

        private void PopulateListBox(List<string> images)
        {
            int index = 0;

            foreach (string s in images)
            {
                using (FileStream fs = new FileStream(s, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = 300;
                    //img.UriSource = new Uri(s, UriKind.Absolute);
                    img.StreamSource = fs;
                    img.EndInit();
                    img.Freeze();
                    GifFrame i = new GifFrame
                    {
                        Name = $"Frame{index}",
                        Selected = true,
                        Image = img
                    };
                    items.Add(i);
                    index++;
                }
            }
        }
    }
}
