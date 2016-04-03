﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace ScreenShotterWPF.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        MainWindow mainwindow;
        public MainView()
        {
            InitializeComponent();
            mainwindow = (MainWindow)Application.Current.MainWindow;
        }

        private void tbi_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (mainwindow.Visibility != Visibility.Hidden)
                return;

            mainwindow.Visibility = Visibility.Visible;
            mainwindow.ShowInTaskbar = true;
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate
            {
                mainwindow.WindowState = WindowState.Normal;
                mainwindow.Activate();
            }));
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (mainwindow.Visibility != Visibility.Hidden)
                return;

            mainwindow.Visibility = Visibility.Visible;
            mainwindow.ShowInTaskbar = true;
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate
            {
                mainwindow.WindowState = WindowState.Normal;
                mainwindow.Activate();
            }));
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
