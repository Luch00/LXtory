using System.Windows;
using System.Windows.Controls;

namespace ScreenShotterWPF.Views
{
    /// <summary>
    /// Interaction logic for GifProgressView.xaml
    /// </summary>
    public partial class GifProgressView : UserControl
    {
        public GifProgressView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
