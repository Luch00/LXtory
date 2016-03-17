using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;
using System.Configuration;
using System.Windows.Interop;

namespace ScreenShotterWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow
    {

        private readonly MainViewModel view;

        public MainWindow()
        {   
            InitializeComponent();
            view = new MainViewModel();
            this.DataContext = view;
            //view = this.DataContext as MainViewModel;
            view.PropertyChanged += View_PropertyChanged;
            this.Closed += view.OnWindowClosed;
        }

        private void View_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TaskbarIcon2")
            {
                tbi.Icon = view.TaskbarIcon2 as Icon;
            }
        }

        //public event PropertyChangedEventHandler PropertyChanged;

        /*protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }*/

        public bool ReadCommandLineArgs(IList<string> args)
        {
            return view.PassCommandLineArgs(args);
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

        // Set some default settings values
        //private static void SetDefaults()
        //{
        //    Properties.Settings.Default.filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        //    Properties.Settings.Default.hkFullscreen = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F2));
        //    Properties.Settings.Default.hkCurrentwindow = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F3));
        //    Properties.Settings.Default.hkSelectedarea = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F4));
        //    Properties.Settings.Default.Save();
        //}

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
            view.WindowHandle = helper.Handle;
        }

        #endregion

        // TODO WPF KEY SWITCH BULL -- DONE, UNUSED!!
        // Do things according to the key pressed
        /*public void KeySwitch(Keys k)
        {
            if (Keys.Escape == k)
            {
                try
                {
                    if (overlay_created)
                    {
                        overlay.Close();
                        button3.Content = "Selected Area";
                        overlay_created = false;
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            else if (Keyboard.Modifiers == (Properties.Settings.Default.hkSelectedarea[0] | Properties.Settings.Default.hkSelectedarea[1] | Properties.Settings.Default.hkSelectedarea[2]) && k == Properties.Settings.Default.hkSelectedarea[3])
            {
                USERNOTIFICATIONSTATE state = NotificationState();
                if (state == USERNOTIFICATIONSTATE.QUNS_RUNNING_D3D_FULL_SCREEN)
                {
                    SetStatusBarText("Fullscreen app prevents area capture..");
                    
                }
                else
                {
                    button3.Content = "Pres Esc to cancel...";
                    CapArea();
                }                
            }
        }*/
    }

    // Class for storing image information
    public class XImage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify(string propName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        private string _url;

        private ICommand openLocalCommand;
        private ICommand openBrowserCommand;
        private ICommand copyClipboardCommand;
        [XmlIgnore]
        private bool openLocalEnabled;
        [XmlIgnore]
        private bool openBrowserEnabled;
        [XmlIgnore]
        private bool copyClipboardEnabled;

        public string filename { get; set; }
        public string date { get; set; }
        public DateTime datetime { get; set; }
        [XmlIgnore]
        public byte[] image;
        [XmlIgnore]
        public bool anonupload { get; set; }
        public string url {
            get { return this._url; }
            set
            {
                this._url = value;
                Notify("url");
            }
        }
        public string filepath { get; set; }

        public XImage()
        {
            datetime = DateTime.MinValue;
            openLocalEnabled = false;
            openBrowserEnabled = false;
            copyClipboardEnabled = false;
            openLocalCommand = new RelayCommand(OpenLocalImage, param => true);
            openBrowserCommand = new RelayCommand(OpenInBrowser, param => true);
            copyClipboardCommand = new RelayCommand(CopyToClipboard, param => true);
        }

        [XmlIgnore]
        public ICommand CopyClipboardCommand
        {
            get { return copyClipboardCommand; }
            set { copyClipboardCommand = value; }
        }
        [XmlIgnore]
        public ICommand OpenBrowserCommand
        {
            get { return openBrowserCommand; }
            set { openBrowserCommand = value; }
        }
        [XmlIgnore]
        public ICommand OpenLocalCommand
        {
            get { return openLocalCommand; }
            set { openLocalCommand = value; }
        }
        [XmlIgnore]
        public bool OpenLocalEnabled
        {
            get { return openLocalEnabled; }
            set { openLocalEnabled = value; Notify("OpenLocalEnabled"); }
        }
        [XmlIgnore]
        public bool OpenBrowserEnabled
        {
            get { return openBrowserEnabled; }
            set { openBrowserEnabled = value; Notify("OpenBrowserEnabled"); }
        }
        [XmlIgnore]
        public bool CopyClipboardEnabled
        {
            get { return copyClipboardEnabled; }
            set { copyClipboardEnabled = value; Notify("CopyClipboardEnabled"); }
        }
        
        private void OpenInBrowser(object param)
        {
            Process.Start(this.url);
        }

        private void OpenLocalImage(object param)
        {
            //Process.Start(Path.Combine(Properties.Settings.Default.filePath, x.filename));
            Process.Start(this.filepath);
        }

        private void CopyToClipboard(object param)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetDataObject(this.url);
            }
            catch (Exception ex)
            {
                //RetryClipboard(x.url);
            }
        }
    }

    public class HotKey
    {
        [UserScopedSetting()]
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public int vkKey { get; set; }
        [UserScopedSetting()]
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public bool ctrl { get; set; }
        [UserScopedSetting()]
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public bool alt { get; set; }
        [UserScopedSetting()]
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public bool shift { get; set; }

        public HotKey() { }
        public HotKey(bool ctrl, bool alt, bool shift, int vkKey)
        {
            this.vkKey = vkKey;
            this.ctrl = ctrl;
            this.alt = alt;
            this.shift = shift;
        }

        public override string ToString()
        {
            string hk = "";
            if (ctrl)
            {
                hk += "Ctrl + ";
            }
            if (alt)
            {
                hk += "Alt + ";
            }
            if (shift)
            {
                hk += "Shift + ";
            }
            hk += KeyInterop.KeyFromVirtualKey(vkKey).ToString();
            return hk;
        }
    }
}
