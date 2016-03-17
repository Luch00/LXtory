using System.Windows;
using System.Windows.Controls;

namespace ScreenShotterWPF
{
    /// <summary>
    /// Interaction logic for EncodingProgressWindow.xaml
    /// </summary>
    public partial class EncodingProgressWindow : Window
    {
        public EncodingProgressWindow()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            var btn = sender as Button;
            btn.Command.Execute(btn.CommandParameter);
        }
    }
}
