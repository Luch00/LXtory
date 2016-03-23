using Prism.Mvvm;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ScreenShotterWPF
{
    public class GifFrame : BindableBase
    {
        private bool selected;
        private string name;
        private BitmapImage image;
        private ICommand itemDoubleClickCommand;

        public GifFrame()
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
            set { selected = value; OnPropertyChanged("Selected"); }
        }

        public BitmapImage Image
        {
            get { return image; }
            set { image = value; }
        }
    }
}
