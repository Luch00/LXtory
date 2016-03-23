using System.Windows.Controls;
//using System.Windows.Documents;
using ScreenShotterWPF.ViewModels;

namespace ScreenShotterWPF.Views
{
    /// <summary>
    /// Interaction logic for GifEditorView.xaml
    /// </summary>
    public partial class GifEditorView : UserControl
    {
        public GifEditorView()
        {
            InitializeComponent();
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GifFrame selectedItem = listBox.SelectedItem as GifFrame;
            if (selectedItem != null)
            {
                if (previewImage.Source != null)
                {
                    previewImage.Source = null;
                }
                
                previewImage.Source = selectedItem.Image;
            }
        }
    }
}
