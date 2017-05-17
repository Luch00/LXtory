using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Prism.Mvvm;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using LXtory.Notifications;
using System.Windows.Media;

namespace LXtory.ViewModels
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
        
        public MainLogic Main { get; private set; }

        public ICommand ExitCommand { get; private set; }
        public ICommand OpenCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public ICommand CaptureFullscreenCommand { get; private set; }
        public ICommand CaptureWindowCommand { get; private set; }
        public ICommand CaptureAreaCommand { get; private set; }
        public ICommand CaptureGifCommand { get; private set; }
        public ICommand CaptureD3DImageCommand { get; private set; }

        public InteractionRequest<IConfirmation> SettingsRequest { get; private set; }

        public ICommand RaiseSettingsCommand { get; private set; }

        private const int HOTKEY_1 = 0;
        private const int HOTKEY_2 = 1;
        private const int HOTKEY_3 = 2;
        private const int HOTKEY_4 = 3;
        private const int HOTKEY_5 = 4;

        private MouseKeyHook mHook;
        private readonly Action<bool> mouseAction;
        private int selectedIndex;

        public MainViewModel()
        {
            Main = new MainLogic();
            //GetContent();
            
            areaButtonText = "Select Area";
            windowButtonText = "Select Window";
            gifButtonEnabled = true;
            this.ExitCommand = new DelegateCommand(ExitApplication);
            this.OpenCommand = new DelegateCommand(OpenImageFolder);
            this.DeleteCommand = new DelegateCommand(DeleteItem);
            
            this.CaptureFullscreenCommand = new DelegateCommand(CaptureFullscreen);
            this.CaptureWindowCommand = new DelegateCommand(CaptureWindow);
            this.CaptureAreaCommand = new DelegateCommand(CaptureArea);
            this.CaptureGifCommand = new DelegateCommand(CaptureGif);
            this.CaptureD3DImageCommand = new DelegateCommand(CaptureD3DImage);

            this.RaiseSettingsCommand = new DelegateCommand(RaiseSettings);

            this.SettingsRequest = new InteractionRequest<IConfirmation>();
            
            mouseAction = HookMouseAction;
        }

        private void RaiseSettings()
        {
            SettingsNotification notification = new SettingsNotification()
            {
                Title = "Settings"
            };
            this.SettingsRequest.Raise(
                notification, returned =>
                {
                    if (returned != null && returned.Confirmed)
                    {
                        Properties.Settings.Default.Save();
                        RegisterHotkeys();
                        Main.ToggleClipboardMonitor();
                        Main.CreateSFTPConnectionInfo();
                    }
                });
        }

        private void DeleteItem()
        {
            Main.RemoveXImage(selectedItem);
        }

        private void CaptureGif()
        {
            GifButtonEnabled = false;
            Main.CapGif();
            GifButtonEnabled = true;
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
            mHook = new MouseKeyHook();
            MouseKeyHook.SetAction(mouseAction);
        }

        private void HookMouseAction(bool b)
        {
            if (b)
            {
                NativeMethods.GetCursorPos(out NativeMethods.POINT p);
                Main.CapWindowFromPoint(p.X, p.Y);
            }
            MouseKeyHook.Unhook();
            WindowButtonText = "Select Window";
        }

        private void CaptureArea()
        {
            AreaButtonText = "Esc to cancel..";
            Main.CapArea();
            AreaButtonText = "Select Area";
        }
        
        private static void ExitApplication()
        {
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        private void OpenImageFolder()
        {
            Process.Start(Properties.Settings.Default.filePath);
        }

        public string WindowButtonText
        {
            get { return windowButtonText; }
            set { SetProperty(ref windowButtonText, value); }
        }

        public bool GifButtonEnabled
        {
            get { return gifButtonEnabled; }
            set { SetProperty(ref gifButtonEnabled, value); }
        }

        public string AreaButtonText
        {
            get { return areaButtonText; }
            set { SetProperty(ref areaButtonText, value); }
        }

        public IntPtr WindowHandle
        {
            get { return windowHandle; }
            set { windowHandle = value; InitializeHotkeys(); }
        }

        public BitmapImage DisplayImage
        {
            get { return displayImage; }
            set { SetProperty(ref displayImage, value); }
        }

        public XImage SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                GetPicture(selectedItem);
                //OnPropertyChanged("SelectedItem");
                RaisePropertyChanged("SelectedItem");
            }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { SetProperty(ref selectedIndex, value); }
        }

        private void GetLocalThumbnail(Uri url)
        {
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.None;
            img.DecodePixelWidth = 300;
            img.UriSource = url;
            img.EndInit();
            DisplayImage = img;
        }

        private async void GetWebThumbnail(Uri url)
        {
            if (url.Host == "puush.me" && Properties.Settings.Default.puush_key != string.Empty)
            {
                byte[] bytes;
                using (var wc = new System.Net.WebClient())
                {
                    string id = url.AbsolutePath.TrimStart('/');
                    System.Collections.Specialized.NameValueCollection nv =
                        new System.Collections.Specialized.NameValueCollection
                        {
                            {"i", id},
                            {"k", Properties.Settings.Default.puush_key}
                        };
                    bytes = await wc.UploadValuesTaskAsync("http://puush.me/api/thumb", nv);
                }
                if (bytes.Length <= 0)
                {
                    
                    return;
                }
                using (var stream = new System.IO.MemoryStream(bytes))
                {
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = 300;
                    img.StreamSource = stream;
                    img.EndInit();
                    DisplayImage = img;
                }
            }
            else
            {
                DisplayImage = new BitmapImage(url);
            }
        }

        // Set picturebox image
        private void GetPicture(XImage x)
        {
            if (x == null)
            {
                DisplayImage = null;
                return;
            }

            Uri url;
            if (x.filepath != string.Empty && File.Exists(x.filepath))
            {
                string ext = Path.GetExtension(x.filename);
                if (ImageFileTypes.SupportedTypes.Contains(ext))
                {
                    url = new Uri(x.filepath, UriKind.Absolute);
                    GetLocalThumbnail(url);
                }
            }
            else if (x.thumbnail != string.Empty && !Properties.Settings.Default.disableWebThumbs)
            {
                url = new Uri(x.thumbnail, UriKind.Absolute);
                GetWebThumbnail(url);
            }
            else
            {
                DisplayImage = null;
            }
        }

        //private void GetContent()
        //{
        //    Main.ReadXML();
        //}

        /*public bool PassCommandLineArgs(IList<string> args)
        {
            return Main.ReadCommandLineArgs(args);
        }*/

        private void RegisterHotkeys()
        {
            UnregisterHotKey();
            RegisterHotKey(HOTKEY_1, Properties.Settings.Default.hkFullscreen);
            RegisterHotKey(HOTKEY_2, Properties.Settings.Default.hkCurrentwindow);
            RegisterHotKey(HOTKEY_3, Properties.Settings.Default.hkSelectedarea);
            RegisterHotKey(HOTKEY_4, Properties.Settings.Default.hkD3DCap);
            RegisterHotKey(HOTKEY_5, Properties.Settings.Default.hkGifcapture);
        }

        private void RegisterHotKey(int hotkey_id, HotKey hk)
        {
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
            if (WindowHandle != null)
            {
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_1);
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_2);
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_3);
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_4);
                NativeMethods.UnregisterHotKey(WindowHandle, HOTKEY_5);
            }
            Console.WriteLine(@"Unregistered");
        }

        private void InitializeHotkeys()
        {
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
                        case HOTKEY_5:
                            Main.CapGif();
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
            MainLogic.SetAsComplete();
            UnregisterHotKey();
        }
    }
}
