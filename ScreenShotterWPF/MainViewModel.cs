using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScreenShotterWPF
{
    class MainViewModel : INotifyPropertyChanged
    {
        ObservableCollection<XImage> ximages = new ObservableCollection<XImage>();
        BitmapImage displayImage;
        XImage selectedItem;
        private int progressValue;
        private string statusText;
        private string areaButtonText;
        private string windowButtonText;
        private bool editorEnabled;
        private bool gifButtonEnabled;
        
        private object taskbarIcon2;
        private IntPtr windowHandle;
        private HwndSource _source;
        readonly System.Timers.Timer timer = new System.Timers.Timer();

        public event PropertyChangedEventHandler PropertyChanged;
        private readonly MainLogic main;

        private ICommand exitCommand;
        private ICommand openCommand;
        private ICommand openSettingsCommand;
        
        private ICommand captureFullscreenCommand;
        private ICommand captureWindowCommand;
        private ICommand captureAreaCommand;
        private ICommand captureGifCommand;
        private ICommand captureD3DImageCommand;

        private const int HOTKEY_1 = 0;
        private const int HOTKEY_2 = 1;
        private const int HOTKEY_3 = 2;
        private const int HOTKEY_4 = 3;

        private KeyHook mHook;
        private readonly Action<bool> mouseAction;

        public MainViewModel()
        {
            main = new MainLogic();
            GetContent();
            TaskbarIcon2 = Properties.Resources.Default;
            
            areaButtonText = "Select Area";
            windowButtonText = "Select Window";
            gifButtonEnabled = true;
            exitCommand = new RelayCommand(ExitApplication, param => true);
            openCommand = new RelayCommand(OpenImageFolder, param => true);
            openSettingsCommand = new RelayCommand(OpenSettings, param => true);
            
            captureFullscreenCommand = new RelayCommand(CaptureFullscreen, param => true);
            captureWindowCommand = new RelayCommand(CaptureWindow, param => true);
            captureAreaCommand = new RelayCommand(CaptureArea, param => true);
            captureGifCommand = new RelayCommand(CaptureGif, param => true);
            captureD3DImageCommand = new RelayCommand(CaptureD3DImage, param => true);
            main.PropertyChanged += Main_PropertyChanged;
            mouseAction = HookMouseAction;
            timer.Interval = 5000;
            timer.Elapsed += timerTick_DelayIconChange;
        }

        private void Main_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ProgressValue")
            {
                ProgressAndIconChange(main.ProgressValue);
            }
            if (e.PropertyName == "StatusText")
            {
                StatusText = main.StatusText;
            }
            if (e.PropertyName == "TrayIcon")
            {
                ChangeTrayIcon(main.TrayIcon);
            }
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ICommand ExitCommand
        {
            get { return exitCommand; }
            set { exitCommand = value; }
        }

        public ICommand OpenCommand
        {
            get { return openCommand; }
            set { openCommand = value; }
        }

        public ICommand OpenSettingsCommand
        {
            get { return openSettingsCommand; }
            set { openSettingsCommand = value; }
        }

        public ICommand CaptureFullscreenCommand
        {
            get { return captureFullscreenCommand; }
            set { captureFullscreenCommand = value; }
        }

        public ICommand CaptureWindowCommand
        {
            get { return captureWindowCommand; }
            set { captureWindowCommand = value; }
        }

        public ICommand CaptureAreaCommand
        {
            get { return captureAreaCommand; }
            set { captureAreaCommand = value; }
        }

        public ICommand CaptureGifCommand
        {
            get { return captureGifCommand; }
            set { captureGifCommand = value; }
        }

        public ICommand CaptureD3DImageCommand
        {
            get { return captureD3DImageCommand; }
            set { captureD3DImageCommand = value; }
        }

        private void CaptureD3DImage(object param)
        {
            main.D3DCapPrimaryScreen();
        }

        private void CaptureFullscreen(object param)
        {
            main.CapFullscreen();
        }

        private void CaptureWindow(object param)
        {
            // mouse hook and such
            WindowButtonText = "Click a Window..";
            mHook = new KeyHook();
            KeyHook.SetAction(mouseAction);
        }

        private void HookMouseAction(bool b)
        {
            if (b)
            {
                NativeMethods.POINT p;
                NativeMethods.GetCursorPos(out p);
                main.CapWindowFromPoint(p.X, p.Y);
            }
            KeyHook.Unhook();
            WindowButtonText = "Select Window";
        }

        private void CaptureArea(object param)
        {
            AreaButtonText = "Esc to cancel..";
            main.CapArea();
            AreaButtonText = "Select Area";
        }

        private async void CaptureGif(object param)
        {
            GifButtonEnabled = false;
            GifOverlayViewModel gv = new GifOverlayViewModel();
            GifOverlay go = new GifOverlay
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true,
                DataContext = gv
            };
            go.ShowDialog();
            if (go.DialogResult.HasValue && go.DialogResult.Value)
            {
                int x = (int)go.Left;
                int y = (int)go.Top;
                int w = gv.WindowWidth;
                int h = gv.WindowHeight;
                int f = gv.GifFramerate;
                int d = gv.GifDuration;
                await main.CapGif(x, y, w, h, f, d, 0);
            }
            else
            {
                go = null;
                gv = null;
            }
            GifButtonEnabled = true;
        }

        private static void ExitApplication(object param)
        {
            Application.Current.Shutdown();
        }

        private void OpenImageFolder(object param)
        {
            Process.Start(Properties.Settings.Default.filePath);
        }

        private void OpenSettings(object param)
        {
            Settings settings = new Settings();
            settings.ShowDialog();
            if (settings.DialogResult.HasValue && settings.DialogResult.Value)
            {
                Properties.Settings.Default.Save();
                RegisterHotkeys();
            }
        }

        public string WindowButtonText
        {
            get { return windowButtonText; }
            set { windowButtonText = value; RaisePropertyChanged("WindowButtonText"); }
        }

        public bool GifButtonEnabled
        {
            get { return gifButtonEnabled; }
            set { gifButtonEnabled = value; RaisePropertyChanged("GifButtonEnabled"); }
        }

        public string AreaButtonText
        {
            get { return areaButtonText; }
            set { areaButtonText = value; RaisePropertyChanged("AreaButtonText"); }
        }

        public IntPtr WindowHandle
        {
            get { return windowHandle; }
            set { windowHandle = value; InitializeHotkeys(); }
        }

        public ObservableCollection<XImage> Ximages
        {
            get { return ximages; }
            set { ximages = value; RaisePropertyChanged("Ximages"); }
        }

        public BitmapImage DisplayImage
        {
            get { return displayImage; }
            set { displayImage = value; RaisePropertyChanged("DisplayImage"); }
        }

        public XImage SelectedIndex
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                CheckContextMenuItems();
                DisplayImage = MainLogic.GetPicture(selectedItem);
                RaisePropertyChanged("SelectedIndex");
            }
        }

        public int ProgressValue
        {
            get { return progressValue; }
            set { progressValue = value; RaisePropertyChanged("ProgressValue"); }
        }

        public string StatusText
        {
            get { return statusText; }
            set { statusText = value; RaisePropertyChanged("StatusText"); }
        }

        public MainLogic GetMainLogic
        {
            get { return main; }
        }

        public bool EditorEnabled
        {
            get { return editorEnabled; }
            set
            {
                editorEnabled = value;
                main.EditorEnabled = value;
                RaisePropertyChanged("EditorEnabled");
            }
        }

        public object TaskbarIcon2
        {
            get { return taskbarIcon2; }
            set { taskbarIcon2 = value; RaisePropertyChanged("TaskbarIcon2"); }
        }

        private void GetContent()
        {
            Ximages = main.ReadXML();
        }

        private void CheckContextMenuItems()
        {
            SelectedIndex.OpenLocalEnabled = SelectedIndex.filepath != string.Empty && File.Exists(SelectedIndex.filepath);

            SelectedIndex.OpenBrowserEnabled = SelectedIndex.CopyClipboardEnabled = (SelectedIndex.url != string.Empty);
        }

        private void ProgressAndIconChange(int pctComplete)
        {
            ProgressValue = pctComplete;
            if (pctComplete >= 10 && pctComplete < 20)
            {
                ChangeTrayIcon("10");
            }
            else if (pctComplete >= 20 && pctComplete < 30)
            {
                ChangeTrayIcon("20");
            }
            else if (pctComplete >= 30 && pctComplete < 40)
            {
                ChangeTrayIcon("30");
            }
            else if (pctComplete >= 40 && pctComplete < 50)
            {
                ChangeTrayIcon("40");
            }
            else if (pctComplete >= 50 && pctComplete < 60)
            {
                ChangeTrayIcon("50");
            }
            else if (pctComplete >= 60 && pctComplete < 70)
            {
                ChangeTrayIcon("60");
            }
            else if (pctComplete >= 70 && pctComplete < 80)
            {
                ChangeTrayIcon("70");
            }
            else if (pctComplete >= 80 && pctComplete < 90)
            {
                ChangeTrayIcon("80");
            }
            else if (pctComplete >= 90)
            {
                ChangeTrayIcon("90");
            }
        }

        private void ChangeTrayIcon(string ico)
        {
            lock (timer)
            {
                timer.Stop();
            }
            switch (ico)
            {
                case "R":
                    TaskbarIcon2 = Properties.Resources.R;
                    break;
                case "F":
                    TaskbarIcon2 = Properties.Resources.F;
                    lock (timer)
                    {
                        timer.Start();
                    }
                    break;
                case "E":
                    TaskbarIcon2 = Properties.Resources.E;
                    break;
                case "Default":
                    TaskbarIcon2 = Properties.Resources.Default;
                    break;
                case "00":
                    TaskbarIcon2 = Properties.Resources._00;
                    break;
                case "10":
                    TaskbarIcon2 = Properties.Resources._10;
                    break;
                case "20":
                    TaskbarIcon2 = Properties.Resources._20;
                    break;
                case "30":
                    TaskbarIcon2 = Properties.Resources._30;
                    break;
                case "40":
                    TaskbarIcon2 = Properties.Resources._40;
                    break;
                case "50":
                    TaskbarIcon2 = Properties.Resources._50;
                    break;
                case "60":
                    TaskbarIcon2 = Properties.Resources._60;
                    break;
                case "70":
                    TaskbarIcon2 = Properties.Resources._70;
                    break;
                case "80":
                    TaskbarIcon2 = Properties.Resources._80;
                    break;
                case "90":
                    TaskbarIcon2 = Properties.Resources._90;
                    break;
            }
        }

        private void timerTick_DelayIconChange(object sender, EventArgs e)
        {
            lock (timer)
            {
                timer.Stop();
                ChangeTrayIcon("Default");
            }
        }

        public bool PassCommandLineArgs(IList<string> args)
        {
            return main.ReadCommandLineArgs(args);
        }

        private void RegisterHotkeys()
        {
            UnregisterHotKey();
            RegisterHotKey(HOTKEY_1, Properties.Settings.Default.hkFullscreen);
            RegisterHotKey(HOTKEY_2, Properties.Settings.Default.hkCurrentwindow);
            RegisterHotKey(HOTKEY_3, Properties.Settings.Default.hkSelectedarea);
            RegisterHotKey(HOTKEY_4, Properties.Settings.Default.hkD3DCap);
        }

        private void RegisterHotKey(int hotkey_id, HotKey hk)
        {
            //var helper = new WindowInteropHelper(UI);
            if (WindowHandle != null && hk != null)
            {
                uint VK_KEY = Convert.ToUInt32(hk.vkKey);
                uint MOD_KEY = 0x4000;
                if (hk.alt)
                {
                    MOD_KEY = 0x0001;
                }
                if (hk.ctrl)
                {
                    MOD_KEY = MOD_KEY | 0x0002;
                }
                if (hk.shift)
                {
                    MOD_KEY = MOD_KEY | 0x0004;
                }
                if (!NativeMethods.RegisterHotKey(WindowHandle, hotkey_id, MOD_KEY, VK_KEY))
                {
                    Console.WriteLine(@"ERROR");
                } 
            }
            Console.WriteLine(@"Registered");
        }

        private void UnregisterHotKey()
        {
            //var helper = new WindowInteropHelper(this);
            if (WindowHandle != null)
            {
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_1);
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_2);
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_3);
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_4);
            }
            Console.WriteLine(@"Unregistered");
        }

        private void InitializeHotkeys()
        {
            //var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(WindowHandle);
            _source.AddHook(HwndHook);
            RegisterHotkeys();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_1:
                            main.CapFullscreen();
                            handled = true;
                            break;

                        case HOTKEY_2:
                            main.CapWindow();
                            handled = true;
                            break;

                        case HOTKEY_3:
                            main.CapArea();
                            handled = true;
                            break;
                       case HOTKEY_4:
                            main.D3DCapPrimaryScreen();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public void OnWindowClosed(object sender, EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            main.SetAsComplete();
            UnregisterHotKey();
        }
    }

    public class RelayCommand : ICommand
    {
        private Action<object> execute;

        private Predicate<object> canExecute;

        private event EventHandler CanExecuteChangedInternal;

        public RelayCommand(Action<object> execute)
            : this(execute, DefaultCanExecute)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            if (canExecute == null)
            {
                throw new ArgumentNullException("canExecute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                this.CanExecuteChangedInternal += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
                this.CanExecuteChangedInternal -= value;
            }
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute != null && this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }

        public void OnCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChangedInternal;
            //DispatcherHelper.BeginInvokeOnUIThread(() => handler.Invoke(this, EventArgs.Empty));
            handler?.Invoke(this, EventArgs.Empty);
        }

        public void Destroy()
        {
            this.canExecute = _ => false;
            this.execute = _ => { return; };
        }

        private static bool DefaultCanExecute(object parameter)
        {
            return true;
        }
    }
}
