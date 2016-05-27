using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;

namespace ScreenShotterWPF.Views
{
    /// <summary>
    /// Interaction logic for GifOverlayView.xaml
    /// </summary>
    public partial class GifOverlayView : UserControl
    {
        public GifOverlayView()
        {
            InitializeComponent();
        }

        private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Window.GetWindow(this)?.DragMove();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            WindowChrome Resizable_BorderLess_Chrome = new WindowChrome();
            Resizable_BorderLess_Chrome.GlassFrameThickness = new Thickness(0);
            Resizable_BorderLess_Chrome.CornerRadius = new CornerRadius(0);
            Resizable_BorderLess_Chrome.CaptionHeight = 5.0;
            WindowChrome.SetWindowChrome(Window.GetWindow(this), Resizable_BorderLess_Chrome);
        }
    }
}
