using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using ScreenShotterWPF.Notifications;

namespace ScreenShotterWPF
{
    class MainLogic : BindableBase
    {
        private ObservableCollection<XImage> ximages = new ObservableCollection<XImage>();

        private readonly BlockingCollection<XImage> queue = new BlockingCollection<XImage>();

        private readonly Dictionary<string, BitmapImage> trayicons = new Dictionary<string, BitmapImage>();
        
        private int uploading = 0;
        private int totalUploading = 0;
        private bool refreshing = false;
        
        readonly Uploader uploader;
        
        readonly Action<int> progressBarUpdate;
        //readonly Action<XImage, string> addXImageToList;

        // For selecting window to capture
        private MouseKeyHook mHook;
        private readonly Action<bool> mouseAction;

        protected Thread clipboardThread;

        private bool overlay_created;

        private int progressValue;
        private string statusText;

        private bool gifCapturing;

        private static readonly List<string> ImageExtensions = new List<string> { ".jpg", ".jpeg", ".bmp", ".gif", ".png" };

        private readonly SynchronizationContext uiContext;

        private readonly bool windows8;
        
        public InteractionRequest<IConfirmation> OverlayRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifOverlayRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifEditorRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifProgressRequest { get; private set; }

        readonly System.Timers.Timer timer = new System.Timers.Timer();

        public MainLogic()
        {
            windows8 = CheckIfWin8OrHigher();
            uiContext = SynchronizationContext.Current;
            progressBarUpdate = ProgressAndIconChange;
            //addXImageToList = AddXimageToList;
            mouseAction = HookMouseAction;
            uploader = new Uploader(progressBarUpdate);
            OverlayRequest = new InteractionRequest<IConfirmation>();
            this.GifOverlayRequest = new InteractionRequest<IConfirmation>();
            this.GifEditorRequest = new InteractionRequest<IConfirmation>();
            this.GifProgressRequest = new InteractionRequest<IConfirmation>();
            LoadIcons();
            SetIcon("Default");
            timer.Interval = 5000;
            timer.Elapsed += timerTick_DelayIconChange;
            if (Properties.Settings.Default.filePath == "")
            {
                SetDefaults();
            }
            StartUploads();
        }

        private static bool CheckIfWin8OrHigher()
        {
            Version win8 = new Version(6, 2, 9200, 0);
            return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version >= win8;
        }

        private void LoadIcons()
        {
            string[] ico = { "Default", "F", "E", "R", "10", "20", "30", "40", "50", "60", "70", "80", "90" };
            foreach (var i in ico)
            {
                var bitmapImage = new BitmapImage(new Uri($"pack://application:,,,/Resources/{i}.ico", UriKind.Absolute));
                bitmapImage.Freeze();
                trayicons.Add(i, bitmapImage);
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

        public bool ReadCommandLineArgs(IList<string> args)
        {
            if (args.Count == 0 || args == null)
                return true;

            if (args.Count > 1)
            {
                for (int i = 1; i < args.Count; i++)
                {
                    string extension = Path.GetExtension(args[i]);
                    if (extension != null && ImageExtensions.Contains(extension.ToLowerInvariant()))
                    {
                        XImage img = new XImage();
                        img.filename = Path.GetFileName(args[i]);
                        img.filepath = args[i];
                        string p = "dd.MM.yy HH:mm:ss";
                        img.datetime = DateTime.Now;
                        string d = DateTime.Now.ToString(p);
                        img.date = d;
                        img.anonupload = Properties.Settings.Default.anonUpload;
                        AddToQueue(img);
                    }
                }
            }

            return true;
        }

        private async void StartUploads()
        {
            var uploadTask = new Task(() => Upload());
            uploadTask.Start();
            Console.WriteLine(@"Uploads Started");
            await uploadTask;
            Console.WriteLine(@"Everything was finished");
        }

        public void SetAsComplete()
        {
            queue.CompleteAdding();
        }

        public void RemoveXImage(XImage selected)
        {
            lock(ximages)
            {
                ximages.Remove(selected);
                WriteXML();
            }
        }

        private void AddToQueue(XImage x)
        {
            try
            {
                queue.Add(x);
                totalUploading++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static bool TokenNeedsRefresh()
        {
            if (Properties.Settings.Default.lastRefreshTime != null)
            {
                TimeSpan diff = DateTime.Now.Subtract(Properties.Settings.Default.lastRefreshTime);
                //return diff.TotalSeconds >= Properties.Settings.Default.imgurTokenExpire;
                return diff.TotalSeconds >= 3600; // BECAUSE IMGUR FAILS
            }
            return true;
        }

        private async void Upload()
        {
            while (!queue.IsCompleted)
            {
                if (!refreshing)
                {
                    XImage currentUpload;
                    if (queue.TryTake(out currentUpload, Timeout.Infinite))
                    {
                        try
                        {
                            if (TokenNeedsRefresh() && currentUpload.anonupload == false)
                            {
                                SetStatusBarText("Refreshing Imgur login..");
                                ChangeTrayIcon("R");
                                await uploader.RefreshToken();
                            }
                            if (currentUpload.image == null)
                            {
                                ReadImageBytes(currentUpload);
                            }
                            if (currentUpload.image == null)
                            {
                                Console.WriteLine(@"Tried to upload an empty image");
                                continue;
                            }
                            uploading++;
                            SetStatusBarText("Uploading.." + uploading + "/" + totalUploading);
                            Tuple<bool, string> result;
                            switch (Properties.Settings.Default.upload_site)
                            {
                                case 0:
                                default:
                                    if (((currentUpload.image.Length / 1024f) / 1024f) > 10)
                                    {
                                        totalUploading--;
                                        currentUpload.image = null;
                                        SetStatusBarText("File too large. Skipping.");
                                        continue;
                                    }
                                    result = uploader.HttpWebRequestUpload(currentUpload);
                                    break;
                                case 1:
                                    if (((currentUpload.image.Length / 1024f) / 1024f) > 20)
                                    {
                                        totalUploading--;
                                        currentUpload.image = null;
                                        SetStatusBarText("File too large. Skipping.");
                                        continue;
                                    }
                                    result = uploader.HttpGyazoWebRequestUpload(currentUpload);
                                    break;
                                case 2:
                                    if (((currentUpload.image.Length / 1024f) / 1024f) > 20)
                                    {
                                        totalUploading--;
                                        currentUpload.image = null;
                                        SetStatusBarText("File too large. Skipping.");
                                        continue;
                                    }
                                    result = uploader.PuushHttpWebRequestUpload(currentUpload);
                                    break;
                            }

                            if (result.Item1)
                            {
                                uiContext.Post(x => AddXimageToList(currentUpload, result.Item2), null);
                                //AddXimageToList(currentUpload, result.Item2);
                                ChangeTrayIcon("F");
                            }
                            else
                            {
                                ChangeTrayIcon("E");
                                MessageBox.Show(result.Item2, "Something went wrong.", MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);
                            }

                            if (queue.Count == 0)
                            {
                                uploading = 0;
                                totalUploading = 0;
                                SetStatusBarText("Done");
                            }
                        }
                        catch (Exception e)
                        {
                            ChangeTrayIcon("E");
                            MessageBox.Show(e.ToString(), "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
            Console.WriteLine(@"STOPPED :O");
        }

        private void ReadImageBytes(XImage currentUpload)
        {
            if (currentUpload.filepath != string.Empty)
            {
                try
                {
                    using (FileStream fileData = File.Open(currentUpload.filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            fileData.CopyTo(ms);
                            currentUpload.image = new byte[ms.Length];
                            currentUpload.image = ms.ToArray();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        // Set some default settings values
        private static void SetDefaults()
        {
            Properties.Settings.Default.filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            /*Properties.Settings.Default.hkFullscreen = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F2));
            Properties.Settings.Default.hkCurrentwindow = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F3));
            Properties.Settings.Default.hkSelectedarea = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F4));
            Properties.Settings.Default.hkGifcapture = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F5));
            Properties.Settings.Default.hkD3DCap = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F6));*/
            Properties.Settings.Default.Save();
        }

        // Put some text in that statusbar
        private void SetStatusBarText(string s)
        {
            StatusText = s;
        }

        public ObservableCollection<XImage> Ximages
        {
            get { return ximages; }
            set { ximages = value; OnPropertyChanged("Ximages"); }
        }

        public int ProgressValue
        {
            get { return progressValue; }
            private set { progressValue = value; OnPropertyChanged("ProgressValue"); }
        }

        public string StatusText
        {
            get { return statusText; }
            private set { statusText = value; OnPropertyChanged("StatusText"); }
        }

        private ImageSource icon;

        public ImageSource Icon
        {
            get { return icon; }
            set
            {
                if (value != icon)
                {
                    icon = value;
                    OnPropertyChanged("Icon"); 
                }
            }
        }

        private void SetIcon(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                Icon = trayicons[s];
            }
            else
            {
                Icon = trayicons["Default"];
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
                    SetIcon("R");
                    break;
                case "F":
                    SetIcon("F");
                    lock (timer)
                    {
                        timer.Start();
                    }
                    break;
                case "E":
                    SetIcon("E");
                    break;
                case "Default":
                    SetIcon("Default");
                    break;
                case "00":
                    SetIcon("00");
                    break;
                case "10":
                    SetIcon("10");
                    break;
                case "20":
                    SetIcon("20");
                    break;
                case "30":
                    SetIcon("30");
                    break;
                case "40":
                    SetIcon("40");
                    break;
                case "50":
                    SetIcon("50");
                    break;
                case "60":
                    SetIcon("60");
                    break;
                case "70":
                    SetIcon("70");
                    break;
                case "80":
                    SetIcon("80");
                    break;
                case "90":
                    SetIcon("90");
                    break;
            }
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

        private void HookMouseAction(bool b)
        {
            if (b)
            {
                NativeMethods.POINT p;
                NativeMethods.GetCursorPos(out p);
                CapWindowFromPoint(p.X, p.Y);
            }
            MouseKeyHook.Unhook();
        }

        public void D3DCapPrimaryScreen()
        {
            if (!windows8)
            {
                MessageBox.Show("DirectX capture requires Windows 8 or higher.", "Fail", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
                
            try
            {
                if (!Properties.Settings.Default.d3dAllScreens)
                {
                    //DesktopDuplication.DuplicatePrimaryScreen();
                    //GC.Collect();
                    ImageManager(DesktopDuplication.DuplicatePrimaryScreen(), "D3DScreenshot");
                }
                else
                {
                    ImageManager(DesktopDuplication.DuplicateAllScreens(), "D3DScreenshot");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error);   
            }
        }

        // Capture whole screen area
        public void CapFullscreen()
        {
            /*if (Properties.Settings.Default.d3dAutoDetect && NotificationState() == NativeMethods.USERNOTIFICATIONSTATE.QUNS_RUNNING_D3D_FULL_SCREEN)
            {
                Console.WriteLine("D3DFullscreen Detected!");
                D3DCapPrimaryScreen();
                return;
            }*/
            
            var top = SystemParameters.VirtualScreenTop;
            var left = SystemParameters.VirtualScreenLeft;
            var w = SystemParameters.VirtualScreenWidth;
            var h = SystemParameters.VirtualScreenHeight;
            ScreenCap((int)w, (int)h, (int)left, (int)top, "Fullscreen");
        }

        // Capture current selected window
        public void CapWindow()
        {
            if (Properties.Settings.Default.d3dAutoDetect && NotificationState() == NativeMethods.USERNOTIFICATIONSTATE.QUNS_RUNNING_D3D_FULL_SCREEN)
            {
                Console.WriteLine("D3DFullscreen Detected!");
                D3DCapPrimaryScreen();
                return;
            }

            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            NativeMethods.RECT rect = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(hWnd, ref rect);

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            ScreenCap(width, height, rect.Left, rect.Top, GetActiveWindowTitle());
        }

        public void CapWindowFromPoint(int x, int y)
        {
            if (Properties.Settings.Default.d3dAutoDetect && NotificationState() == NativeMethods.USERNOTIFICATIONSTATE.QUNS_RUNNING_D3D_FULL_SCREEN)
            {
                Console.WriteLine("D3DFullscreen Detected!");
                D3DCapPrimaryScreen();
                return;
            }

            IntPtr hWnd = NativeMethods.WindowFromPoint(x, y);
            NativeMethods.RECT rect = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(hWnd, ref rect);

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            string title = "null";
            if (NativeMethods.GetWindowText(hWnd, buff, nChars) > 0)
            {
                title = buff.ToString();
            }

            ScreenCap(width, height, rect.Left, rect.Top, title);
        }

        // Create an overlay, draw a rectangle on the overlay to cap that area
        public void CapArea()
        {
            /*if (Properties.Settings.Default.d3dAutoDetect && NotificationState() == NativeMethods.USERNOTIFICATIONSTATE.QUNS_RUNNING_D3D_FULL_SCREEN)
            {
                Console.WriteLine("D3DFullscreen Detected!");
                D3DCapPrimaryScreen();
                return;
            }*/

            if (!overlay_created)
            {
                overlay_created = true;
                OverlayNotification notification = new OverlayNotification();
                notification.Title = "Overlay";
                notification.WindowTop = SystemParameters.VirtualScreenTop;
                notification.WindowLeft = SystemParameters.VirtualScreenLeft;
                notification.WindowWidth = SystemParameters.VirtualScreenWidth;
                notification.WindowHeight = SystemParameters.VirtualScreenHeight;
                this.OverlayRequest.Raise(
                    notification, returned =>
                    {
                        if (returned != null && returned.Confirmed)
                        {
                            ScreenCap((int)notification.Rect.Width, (int)notification.Rect.Height, (int)notification.Rect.X, (int)notification.Rect.Y, "AreaCap");
                        }
                        overlay_created = false;
                    });
            }
        }

        public async void CapGif()
        {
            if (gifCapturing)
                return;

            gifCapturing = true;
            GifOverlayNotification notification = new GifOverlayNotification();
            notification.Title = "GifOverlay";
            var returned = await this.GifOverlayRequest.RaiseAsync(notification);
            if (returned != null && returned.Confirmed)
            {
                Console.WriteLine("TEST");

                int x = notification.WindowLeft;
                int y = notification.WindowTop;
                int w = notification.WindowWidth;
                int h = notification.WindowHeight;
                int f = notification.GifFramerate;
                int d = notification.GifDuration;
                Gif gif = new Gif(f, d, w, h, x, y);
                await gif.StartCapture();
                if (gif.Frames.Count > 0)
                {
                    bool cancelled = false;
                    if (Properties.Settings.Default.gifEditorEnabled)
                    {
                        GifEditorNotification gen = new GifEditorNotification();
                        gen.Title = "Gif Editor";
                        gen.Gif = gif;
                        var gen_returned = await this.GifEditorRequest.RaiseAsync(gen);
                        if (gen_returned != null && !gen_returned.Confirmed)
                        {
                            cancelled = true;
                        }
                    }
                    if (!cancelled)
                    {
                        GifProgressNotification gpn = new GifProgressNotification();
                        gpn.Title = "Encoding Gif..";
                        gpn.Gif = gif;
                        var gpn_returned = await this.GifProgressRequest.RaiseAsync(gpn);
                        if (gpn_returned != null && gpn_returned.Confirmed)
                        {
                            var filename = gpn.Name;
                            if (filename != string.Empty)
                            {
                                XImage img = new XImage();
                                img.filename = filename;
                                img.filepath = Path.Combine(Properties.Settings.Default.filePath, filename);
                                string p = "dd.MM.yy HH:mm:ss";
                                img.datetime = DateTime.Now;
                                string date = DateTime.Now.ToString(p);
                                img.date = date;

                                if (Properties.Settings.Default.gifUpload)
                                {
                                    img.anonupload = Properties.Settings.Default.anonUpload;
                                    AddToQueue(img);
                                }
                                else
                                {
                                    //addXImageToList(img, "");
                                    AddXimageToList(img, "");
                                }
                            }
                        }
                    }
                }
            }
            gifCapturing = false;
        }

        // Capture an area of the screen, save as PNG
        private void ScreenCap(int width, int height, int rX, int rY, string filename)
        {
            if (width > 0 && height > 0)
            {
                using (Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (var gfx = Graphics.FromImage(bmp))
                    {
                        gfx.CopyFromScreen(rX,
                                           rY,
                                           0,
                                           0,
                                           new System.Drawing.Size(width, height),
                                           CopyPixelOperation.SourceCopy);
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        ImageManager(ms.ToArray(), filename);
                    }
                }
            }
        }

        // Get active window title to be used in filenames
        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            var handle = NativeMethods.GetForegroundWindow();
            
            return NativeMethods.GetWindowText(handle, buff, nChars) > 0 ? buff.ToString() : "null";
        }

        // Strip illegal characters from filename
        private string MakeValidFileName(string name)
        {
            var builder = new StringBuilder();
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var cur in name)
            {
                if (!invalid.Contains(cur))
                {
                    builder.Append(cur);
                }
            }
            return builder.ToString();
        }

        private string SaveImageToDisk(byte[] bmp, string filename)
        {
            int i = 1;
            filename = MakeValidFileName(filename);

            // Create directory if doesnt exist yet
            if (!Directory.Exists(Properties.Settings.Default.filePath))
            {
                try
                {
                    Directory.CreateDirectory(Properties.Settings.Default.filePath);
                }
                catch (Exception e)
                {
                    StatusText = e.Message;
                    return string.Empty;
                }
            }
            string file = $"{filename}.png";
            string target = Path.Combine(Properties.Settings.Default.filePath, file);
            // if file with same name exists append a number to it
            while (File.Exists(target))
            {
                i++;
                file = $"{filename}({i}).png";
                target = Path.Combine(Properties.Settings.Default.filePath, file);
            }

            try
            {
                File.WriteAllBytes(target, bmp);
            }
            catch (Exception e)
            {
                StatusText = e.Message;
                return string.Empty;
            }

            return target;
        }

        private void ImageManager(byte[] image, string filename)
        {
            const string datePattern = @"dd-MM-yy_HH-mm-ss";
            string date = DateTime.Now.ToString(datePattern);
            string f = filename + "_" + date;

            XImage x = new XImage
            {
                image = image,
                filename = f + ".png",
                url = "",
                filepath = ""
            };
            const string p = "dd.MM.yy HH:mm:ss";
            x.datetime = DateTime.Now;
            string d = DateTime.Now.ToString(p);
            x.date = d;

            if (Properties.Settings.Default.saveLocal)
            {
                x.filepath = SaveImageToDisk(x.image, filename);
                x.filename = Path.GetFileName(x.filepath);
                lock (ximages)
                {
                    ximages.Add(x);
                    WriteXML(); 
                }
            }

            if (Properties.Settings.Default.autoUpload)
            {
                x.anonupload = Properties.Settings.Default.anonUpload;
                AddToQueue(x);
            }
            else
            {
                x.image = null;
            }
        }

        // Add image to list
        private void AddXimageToList(XImage x, string url)
        {
            if (x == null)
            {
                Console.WriteLine("ERROR");
                return;
            }
            if (!Properties.Settings.Default.saveLocal && x.filepath.Length == 0)
            {
                x.url = url;
                lock (ximages)
                {
                    ximages.Add(x);
                    WriteXML(); 
                }
            }
            else
            {
                XImage y = (from i in ximages
                            where i == x
                            select i).FirstOrDefault();
                if (y != null)
                {
                    y.url = url;
                    WriteXML();
                }
                else
                {
                    x.url = url;
                    lock (ximages)
                    {
                        ximages.Add(x);
                        WriteXML(); 
                    }
                }
            }
            if (url.Length > 0)
            {
                if (Properties.Settings.Default.openInBrowser)
                {
                    Process.Start(url);
                }
                if (Properties.Settings.Default.lastToClipboard)
                {
                    clipboardThread = new Thread(copy_to_clipboard);
                    clipboardThread.SetApartmentState(ApartmentState.STA);
                    clipboardThread.Start(url);
                }
            }
        }

        // Function to use as a thread to put stuff on clipboard
        private void copy_to_clipboard(object state)
        {
            var url = (string)state;
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(url);
            }
            catch (Exception)
            {
                RetryClipboard(url);
            }
        }

        private void RetryClipboard(string obj)
        {
            for (int i = 0; i < 9; i++)
            {
                try
                {
                    Clipboard.Clear();
                    Clipboard.SetText(obj);
                    return;
                }
                catch (Exception)
                {
                }
            }
            //statusChange("Clipboard copy failed.");
            StatusText = "Clipboard copy failed.";
        }

        // Set picturebox image only for local and imgur hosted images
        public static BitmapImage GetPicture(XImage x)
        {
            if (x == null)
                return null;

            string url;
            if (x.filepath != string.Empty && File.Exists(x.filepath))
            {
                url = x.filepath;
            }
            else if(x.url != string.Empty)
            {
                Uri uri = new Uri(x.url);
                if (uri.Host == "i.imgur.com")
                {
                    int i = x.url.LastIndexOf('.');
                    url = x.url.Substring(0, i) + "m" + x.url.Substring(i);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.None;
            img.DecodePixelWidth = 300;
            img.UriSource = new Uri(url, UriKind.Absolute);
            img.EndInit();
            return img;
        }

        // Write history xml
        private void WriteXML()
        {
            string f = Path.Combine((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)), @"Luch\LxTory\images.xml");
            if (!Directory.Exists(Path.Combine((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)), @"Luch\LxTory")))
            {
                Directory.CreateDirectory(Path.Combine((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)), @"Luch\LxTory"));
            }
            XmlSerializer s = new XmlSerializer(typeof(ObservableCollection<XImage>));
            TextWriter w = new StreamWriter(f);
            s.Serialize(w, ximages);
            w.Close();
        }

        // Read the history xml
        public void ReadXML()
        {
            string f = Path.Combine((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)), @"Luch\LxTory\images.xml");
            XmlSerializer s = new XmlSerializer(typeof(ObservableCollection<XImage>));
            if (File.Exists(f))
            {
                using (FileStream fs = new FileStream(f, FileMode.Open))
                {
                    ximages = (ObservableCollection<XImage>)s.Deserialize(fs);
                    fs.Close();
                }
            }
        }

        private static NativeMethods.USERNOTIFICATIONSTATE NotificationState()
        {
            NativeMethods.USERNOTIFICATIONSTATE state;
            var value = NativeMethods.SHQueryUserNotificationState(out state);
            return state;
        }
    }
}
