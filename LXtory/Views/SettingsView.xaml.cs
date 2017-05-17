using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using LXtory.ViewModels;

namespace LXtory.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            //Regex regex = new Regex("[^0-9]+");
            //e.Handled = regex.IsMatch(e.Text);
            e.Handled = IsTextAllowed(e.Text);
        }

        private void DataObject_OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                string text = (string) e.DataObject.GetData(typeof (String));
                if (IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]+");
            return regex.IsMatch(text);
        }

        private void PasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = (SettingsViewModel) this.DataContext;
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null) passwordBox.Password = vm.FTPPassword;
        }

        private void PassphraseBox_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = (SettingsViewModel) this.DataContext;
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null) passwordBox.Password = vm.FTPPassphrase;
        }
    }
}
