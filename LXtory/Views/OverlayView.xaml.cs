using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace LXtory.Views
{
    /// <summary>
    /// Interaction logic for OverlayView.xaml
    /// </summary>
    public partial class OverlayView : UserControl
    {
        public OverlayView()
        {
            this.Loaded += OverlayView_Loaded;
            InitializeComponent();
        }

        private void OverlayView_Loaded(object sender, RoutedEventArgs e)
        {
            // Prevent the overlay from stealing focus
            WindowInteropHelper helper = new WindowInteropHelper((Window)this.Parent);
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE, NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE) | NativeMethods.WS_EX_NOACTIVATE);
        }
    }
}
