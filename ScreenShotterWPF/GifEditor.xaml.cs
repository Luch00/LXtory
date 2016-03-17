using System;
using System.Windows;
using System.Windows.Controls;

namespace ScreenShotterWPF
{
    /// <summary>
    /// Interaction logic for GifEditor.xaml
    /// </summary>
    public partial class GifEditor
    {
        public GifEditor()
        {
            InitializeComponent();
            listBox.Focus();
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListItem selectedItem = listBox.SelectedItem as ListItem;
            if (selectedItem != null)
            {
                if (previewImage.Source != null)
                {
                    previewImage.Source = null;
                }

                previewImage.Source = selectedItem.Image;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            /*foreach (var item in items)
            {
                item.Image = null;
            }
            items.Clear();*/
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    /*public class ListItem : INotifyPropertyChanged
    {
        private bool selected;
        private string name;
        private BitmapImage image;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
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
    }*/
}
