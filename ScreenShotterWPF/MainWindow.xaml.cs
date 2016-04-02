using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Windows.Interop;
using ScreenShotterWPF.ViewModels;

namespace ScreenShotterWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow
    {

        private MainViewModel mainview;

        public MainWindow()
        {   
            InitializeComponent();
            
            //view = new MainViewModel();
            //this.DataContext = view;
            //this.Closed += view.OnWindowClosed;
        }

        public bool ReadCommandLineArgs(IList<string> args)
        {
            return mainview.PassCommandLineArgs(args);
        }

        private void Startup_Minimize()
        {
            if (Properties.Settings.Default.startMinimized)
            {
                if (Properties.Settings.Default.minimizeToTray)
                {   
                    this.WindowState = WindowState.Minimized;
                    this.ShowInTaskbar = false;
                    this.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.WindowState = WindowState.Minimized;
                }
            }
        }

        #region EVENTS
        // EVENTS
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Properties.Settings.Default.closeToTray)
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Visibility = Visibility.Hidden;
            }
        }

        private void tbi_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (this.Visibility != Visibility.Hidden)
                return;

            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate
            {
                this.WindowState = WindowState.Normal;
                this.Activate();
            }));
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.Visibility != Visibility.Hidden)
                return;

            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate
            {
                this.WindowState = WindowState.Normal;
                this.Activate();
            }));
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void UI_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized || !Properties.Settings.Default.minimizeToTray)
                return;

            this.ShowInTaskbar = false;
            this.Visibility = Visibility.Hidden;
        }

        private void UI_Loaded(object sender, RoutedEventArgs e)
        {
            Startup_Minimize();
        }

        #endregion

        private void UI_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            
        }

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            mainview = MainView.DataContext as MainViewModel;
            WindowInteropHelper helper = new WindowInteropHelper(this);
            mainview.WindowHandle = helper.Handle;
        }
    }
}
