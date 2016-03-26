using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Prism.Mvvm;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using ScreenShotterWPF.Notifications;

namespace ScreenShotterWPF.ViewModels
{
    class MainViewModel : BindableBase
    {
        BitmapImage displayImage;
        XImage selectedItem;
        private string areaButtonText;
        private string windowButtonText;
        private bool gifButtonEnabled;
        
        private IntPtr windowHandle;
        private HwndSource _source;

        //public event PropertyChangedEventHandler PropertyChanged;
        public MainLogic Main { get; private set; }

        public ICommand ExitCommand { get; private set; }
        public ICommand OpenCommand { get; private set; }
        //public ICommand OpenSettingsCommand { get; private set; }

        public ICommand CaptureFullscreenCommand { get; private set; }
        public ICommand CaptureWindowCommand { get; private set; }
        public ICommand CaptureAreaCommand { get; private set; }
        //public ICommand CaptureGifCommand { get; private set; }
        public ICommand CaptureD3DImageCommand { get; private set; }

        public InteractionRequest<IConfirmation> SettingsRequest { get; private set; } 
        public InteractionRequest<IConfirmation> GifOverlayRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifEditorRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifProgressRequest { get; private set; } 

        public ICommand RaiseSettingsCommand { get; private set; }
        public ICommand RaiseGifOverlayCommand { get; private set; }

        private const int HOTKEY_1 = 0;
        private const int HOTKEY_2 = 1;
        private const int HOTKEY_3 = 2;
        private const int HOTKEY_4 = 3;

        private KeyHook mHook;
        private readonly Action<bool> mouseAction;

        public MainViewModel()
        {
            Main = new MainLogic();
            GetContent();
            
            areaButtonText = "Select Area";
            windowButtonText = "Select Window";
            gifButtonEnabled = true;
            this.ExitCommand = new DelegateCommand(ExitApplication);
            this.OpenCommand = new DelegateCommand(OpenImageFolder);
            //this.OpenSettingsCommand = new DelegateCommand(OpenSettings);
            
            this.CaptureFullscreenCommand = new DelegateCommand(CaptureFullscreen);
            this.CaptureWindowCommand = new DelegateCommand(CaptureWindow);
            this.CaptureAreaCommand = new DelegateCommand(CaptureArea);
            //this.CaptureGifCommand = new DelegateCommand(CaptureGif);
            this.CaptureD3DImageCommand = new DelegateCommand(CaptureD3DImage);

            this.RaiseSettingsCommand = new DelegateCommand(RaiseSettings);
            this.RaiseGifOverlayCommand = new DelegateCommand(RaiseGifOverlay);

            this.SettingsRequest = new InteractionRequest<IConfirmation>();
            this.GifOverlayRequest = new InteractionRequest<IConfirmation>();
            this.GifEditorRequest = new InteractionRequest<IConfirmation>();
            this.GifProgressRequest = new InteractionRequest<IConfirmation>();
            
            mouseAction = HookMouseAction;
        }

        private void RaiseSettings()
        {
            SettingsNotification notification = new SettingsNotification();
            notification.Title = "Settings";
            this.SettingsRequest.Raise(
                notification, returned =>
                {
                    if (returned != null && returned.Confirmed)
                    {
                        Properties.Settings.Default.Save();
                        RegisterHotkeys();
                    }
                });
        }

        private async void RaiseGifOverlay()
        {
            GifOverlayNotification notification = new GifOverlayNotification();
            notification.Title = "GifOverlay";
            var returned = await this.GifOverlayRequest.RaiseAsync(notification);
            if (returned != null && returned.Confirmed)
            {
                Console.WriteLine("TEST");
                GifButtonEnabled = false;
                int x = notification.WindowLeft;
                int y = notification.WindowTop;
                int w = notification.WindowWidth;
                int h = notification.WindowHeight;
                int f = notification.GifFramerate;
                int d = notification.GifDuration;
                Gif gif = new Gif(f, d, w, h, x, y);
                List<string> frames = await gif.StartCapture();
                if (frames.Count > 0)
                {
                    List<string> selected = new List<string>();
                    if (Properties.Settings.Default.gifEditorEnabled)
                    {
                        GifEditorNotification gen = new GifEditorNotification();
                        gen.Title = "Gif Editor";
                        gen.Frames = frames;
                        var gen_returned = await this.GifEditorRequest.RaiseAsync(gen);
                        if (gen_returned != null && gen_returned.Confirmed)
                        {
                            Console.WriteLine(gen.SelectedIndexes.Count.ToString());
                            foreach (int i in gen.SelectedIndexes)
                            {
                                selected.Add(frames[i]);
                            }
                        }
                    }
                    else
                    {
                        selected = frames;
                    }
                    GifProgressNotification gpn = new GifProgressNotification();
                    gpn.Title = "Encoding Gif..";
                    gpn.Progress = 0;
                    gpn.Gif = gif;
                    gpn.Frames = selected;
                    var gpn_returned = await this.GifProgressRequest.RaiseAsync(gpn);
                    if (gpn_returned != null && gpn_returned.Confirmed)
                    {
                        Main.AddGif(gpn.Name);
                    }
                }
                GifButtonEnabled = true;
            }
        }

        private void CaptureD3DImage()
        {
            Main.D3DCapPrimaryScreen();
        }

        private void CaptureFullscreen()
        {
            Main.CapFullscreen();
        }

        private void CaptureWindow()
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
                Main.CapWindowFromPoint(p.X, p.Y);
            }
            KeyHook.Unhook();
            WindowButtonText = "Select Window";
        }

        private void CaptureArea()
        {
            AreaButtonText = "Esc to cancel..";
            Main.CapArea();
            AreaButtonText = "Select Area";
        }

        /*private async void CaptureGif()
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
        }*/
        
        private static void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        private void OpenImageFolder()
        {
            Process.Start(Properties.Settings.Default.filePath);
        }

        /*private void OpenSettings()
        {
            Settings settings = new Settings();
            settings.ShowDialog();
            if (settings.DialogResult.HasValue && settings.DialogResult.Value)
            {
                Properties.Settings.Default.Save();
                RegisterHotkeys();
            }
        }*/

        public string WindowButtonText
        {
            get { return windowButtonText; }
            set { windowButtonText = value; OnPropertyChanged("WindowButtonText"); }
        }

        public bool GifButtonEnabled
        {
            get { return gifButtonEnabled; }
            set { gifButtonEnabled = value; OnPropertyChanged("GifButtonEnabled"); }
        }

        public string AreaButtonText
        {
            get { return areaButtonText; }
            set { areaButtonText = value; OnPropertyChanged("AreaButtonText"); }
        }

        public IntPtr WindowHandle
        {
            get { return windowHandle; }
            set { windowHandle = value; InitializeHotkeys(); }
        }

        public BitmapImage DisplayImage
        {
            get { return displayImage; }
            set { displayImage = value; OnPropertyChanged("DisplayImage"); }
        }

        public XImage SelectedIndex
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                CheckContextMenuItems();
                DisplayImage = MainLogic.GetPicture(selectedItem);
                OnPropertyChanged("SelectedIndex");
            }
        }

        private void GetContent()
        {
            Main.ReadXML();
        }

        private void CheckContextMenuItems()
        {
            SelectedIndex.OpenLocalEnabled = SelectedIndex.filepath != string.Empty && File.Exists(SelectedIndex.filepath);

            SelectedIndex.OpenBrowserEnabled = SelectedIndex.CopyClipboardEnabled = (SelectedIndex.url != string.Empty);
        }

        

        

        

        public bool PassCommandLineArgs(IList<string> args)
        {
            return Main.ReadCommandLineArgs(args);
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
                            Main.CapFullscreen();
                            handled = true;
                            break;

                        case HOTKEY_2:
                            Main.CapWindow();
                            handled = true;
                            break;

                        case HOTKEY_3:
                            Main.CapArea();
                            handled = true;
                            break;
                       case HOTKEY_4:
                            Main.D3DCapPrimaryScreen();
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
            Main.SetAsComplete();
            UnregisterHotKey();
        }
    }
}
