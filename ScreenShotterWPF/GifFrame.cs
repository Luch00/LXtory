using Prism.Mvvm;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Prism.Commands;

namespace ScreenShotterWPF
{
    public class GifFrame : BindableBase
    {
        private bool selected;

        public ICommand ItemDoubleClickCommand { get; private set; }
        public string Name { get; set; }
        public string Filepath { get; set; }
        public BitmapImage Image { get; set; }

        public GifFrame()
        {
            selected = true;
            ItemDoubleClickCommand = new DelegateCommand(CheckUncheckItem);
        }

        private void CheckUncheckItem()
        {
            this.Selected = !this.Selected;
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; OnPropertyChanged("Selected"); }
        }
    }
}
