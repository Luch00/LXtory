using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;

namespace ScreenShotterWPF
{
    /// <summary>
    /// Interaction logic for GifOverlay.xaml
    /// </summary>
    public partial class GifOverlay
    {
        public GifOverlay()
        {
            InitializeComponent();
            WindowChrome Resizable_BorderLess_Chrome = new WindowChrome();
            Resizable_BorderLess_Chrome.GlassFrameThickness = new Thickness(0);
            Resizable_BorderLess_Chrome.CornerRadius = new CornerRadius(0);
            Resizable_BorderLess_Chrome.CaptionHeight = 5.0;
            WindowChrome.SetWindowChrome(this, Resizable_BorderLess_Chrome);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        public object View
        {
            get { return this.DataContext; }
        }
    }
}
