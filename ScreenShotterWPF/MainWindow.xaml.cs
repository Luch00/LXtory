using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Windows.Interop;

namespace ScreenShotterWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow
    {

        //private readonly MainViewModel view;

        public MainWindow()
        {   
            InitializeComponent();
            //view = new MainViewModel();
            //this.DataContext = view;
            //this.Closed += view.OnWindowClosed;
        }

        public bool ReadCommandLineArgs(IList<string> args)
        {
            //return view.PassCommandLineArgs(args);
            return true;


            /*if (args.Count == 0 || args == null)
                return true;

            if (args.Count > 1)
            {
                List<string> ImageExtensions = new List<string> { ".jpg", ".jpeg", ".bmp", ".gif", ".png" };
                for (int i = 1; i < args.Count; i++)
                {
                    if (ImageExtensions.Contains(Path.GetExtension(args[i]).ToLowerInvariant()))
                    {
                        XImage img = new XImage();
                        img.filename = Path.GetFileName(args[i]);
                        img.filepath = args[i];
                        string p = "dd.MM.yy HH:mm:ss";
                        img.datetime = DateTime.Now;
                        string d = DateTime.Now.ToString(p);
                        img.date = d;
                        img.anonupload = Properties.Settings.Default.anonUpload;
                        //imgur.AddToQueue(img);
                    }
                }
            }
            */
            //return true;
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

        // Remove image node from XML file, delete image file
        /*private void DeleteEntry(string filename)
        {
            string f = Path.Combine((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)), @"Luch\LXtory\images.xml");

            if (File.Exists(f))
            {
                XImage x = (from i in ximages
                            where i.filename == filename
                            select i).FirstOrDefault();
                ximages.Remove(x);
                WriteXML();
                File.Delete(Properties.Settings.Default.filePath + @"\" + filename);
            }
        }*/

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
            WindowInteropHelper helper = new WindowInteropHelper(this);
            //view.WindowHandle = helper.Handle;
        }

        #endregion
    }
}
