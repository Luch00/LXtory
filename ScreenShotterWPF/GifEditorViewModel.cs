using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ScreenShotterWPF
{
    internal class GifEditorViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ListItem> items;
        public List<int> selectedIndexes;
        //private int gifQuality;
        private int selectedIndex;
        private ICommand checkBeginningCommand;
        private ICommand checkEndCommand;
        private ICommand uncheckBeginningCommand;
        private ICommand uncheckEndCommand;
        private ICommand encodeCommand;
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public GifEditorViewModel(List<string> images)
        {
            items = new ObservableCollection<ListItem>();
            selectedIndexes = new List<int>();
            //gifQuality = Properties.Settings.Default.gifQuality;
            checkBeginningCommand = new RelayCommand(CheckFromBeginning, CanCheckUncheck);
            checkEndCommand = new RelayCommand(CheckFromEnd, CanCheckUncheck);
            uncheckBeginningCommand = new RelayCommand(UncheckFromBeginning, CanCheckUncheck);
            uncheckEndCommand = new RelayCommand(UncheckFromEnd, CanCheckUncheck);
            encodeCommand = new RelayCommand(StartEncode, param => true);
            PopulateListBox(images);
            selectedIndex = 0;
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

        public ObservableCollection<ListItem> Items
        {
            get { return items; }
            set { items = value; }
        }

        /*public int GifQuality
        {
            get { return gifQuality; }
            set { gifQuality = value; }
        }*/

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; RaisePropertyChanged("SelectedIndex"); }
        }

        private void StartEncode(object param)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Selected)
                {
                    selectedIndexes.Add(i);
                }
            }
            Window w = param as Window;
            w.DialogResult = true;
        }

        private void CheckFromBeginning(object param)
        {   
            CheckUncheckBoxes(true, false);
        }

        private void UncheckFromBeginning(object param)
        {
            CheckUncheckBoxes(false, false);
        }

        private void CheckFromEnd(object param)
        {
            CheckUncheckBoxes(true, true);
        }

        private void UncheckFromEnd(object param)
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

        private bool CanCheckUncheck(object param)
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
                    ListItem i = new ListItem
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

    public class ListItem : INotifyPropertyChanged
    {
        private bool selected;
        private string name;
        private BitmapImage image;
        private ICommand itemDoubleClickCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ListItem()
        {
            itemDoubleClickCommand = new RelayCommand(CheckUncheckItem, param => true);
        }

        public ICommand ItemDoubleClickCommand
        {
            get { return itemDoubleClickCommand; }
            set { itemDoubleClickCommand = value; }
        }

        private void CheckUncheckItem(object param)
        {
            //ListItem i = param as ListItem;
            //i.Selected = !i.Selected;
            this.Selected = !this.Selected;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; RaisePropertyChanged("Selected"); }
        }

        public BitmapImage Image
        {
            get { return image; }
            set { image = value; }
        }
    }
}
