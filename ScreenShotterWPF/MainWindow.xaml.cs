﻿using System;
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

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            mainview = MainView.DataContext as MainViewModel;
            WindowInteropHelper helper = new WindowInteropHelper(this);
            mainview.WindowHandle = helper.Handle;
        }

        #endregion
    }
}
