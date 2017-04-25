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
        //private double top;
        //private double left;
        public GifOverlayView()
        {   
            InitializeComponent();
            //border1.Focus();
            //left = Window.GetWindow(this)?.Left ?? 0;
            //top = Window.GetWindow(this)?.Top ?? 0;
        }

        private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Keyboard.ClearFocus();
            //border1.Focus();            
            Window.GetWindow(this)?.DragMove();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            WindowChrome Resizable_BorderLess_Chrome = new WindowChrome()
            {
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                CaptionHeight = 5.0
            };
            WindowChrome.SetWindowChrome(Window.GetWindow(this), Resizable_BorderLess_Chrome);
        }

        private void Border_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            /*switch (e.Key)
            {
                case Key.None:
                    break;
                case Key.Left:
                    Window.GetWindow(this).Left--;
                    break;
                case Key.Up:
                    Window.GetWindow(this).Top--;
                    break;
                case Key.Right:
                    Window.GetWindow(this).Left++;
                    break;
                case Key.Down:
                    Window.GetWindow(this).Top++;
                    break;
                default:
                    break;
            }*/
            //e.Handled = true;
            //Keyboard.ClearFocus();
            //border1.Focus();
        }
    }
}
