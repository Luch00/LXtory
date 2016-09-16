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
using System.Web.Script.Serialization;
using Microsoft.WindowsAPICodePack.Dialogs;
using Renci.SshNet;

namespace ScreenShotterWPF
{
    class MainLogic : BindableBase
    {
        private ObservableCollection<XImage> ximages = new ObservableCollection<XImage>();

        private static readonly BlockingCollection<XImage> queue = new BlockingCollection<XImage>();

        private readonly Dictionary<string, BitmapImage> trayicons = new Dictionary<string, BitmapImage>();

        private static readonly Properties.Settings settings = Properties.Settings.Default;
        
        private int uploading = 0;
        private int totalUploading = 0;
        private bool refreshing = false;

        //readonly Action<XImage, string> addXImageToList;

        // For selecting window to capture
        private MouseKeyHook mHook;
        private readonly Action<bool> mouseAction;

        protected Thread clipboardThread;

        private bool overlay_created;

        private int progressValue;
        private string statusText;

        private bool gifCapturing;

        //private static readonly List<string> ImageExtensions = new List<string> { ".jpg", ".jpeg", ".bmp", ".gif", ".png" };
        private const string defaultDateTimePattern = @"dd-MM-yy_HH-mm-ss";

        private readonly SynchronizationContext uiContext;

        private readonly bool windows8;
        
        public InteractionRequest<IConfirmation> OverlayRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifOverlayRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifEditorRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifProgressRequest { get; private set; }

        private readonly System.Timers.Timer timer = new System.Timers.Timer();

        private ConnectionInfo ftpConnectionInfo;

        public MainLogic()
        {
            windows8 = CheckIfWin8OrHigher();
            uiContext = SynchronizationContext.Current;
            //addXImageToList = AddXimageToList;
            mouseAction = HookMouseAction;
            //uploader = new Uploader(ProgressAndIconChange);
            Uploader.ProgressBarUpdate = ProgressAndIconChange;
            CreateSFTPConnectionInfo();
            OverlayRequest = new InteractionRequest<IConfirmation>();
            this.GifOverlayRequest = new InteractionRequest<IConfirmation>();
            this.GifEditorRequest = new InteractionRequest<IConfirmation>();
            this.GifProgressRequest = new InteractionRequest<IConfirmation>();
            LoadIcons();
            SetIcon("Default");
            timer.Interval = 5000;
            timer.Elapsed += timerTick_DelayIconChange;
            if (settings.filePath == "")
            {
                SetDefaults();
            }
            StartUploads();
        }

        public void CreateSFTPConnectionInfo()
        {
            if (settings.ftpProtocol == 0)
                return;

            try
            {
                AuthenticationMethod auth;
                if (settings.ftpMethod == 0)
                {
                    auth = new PasswordAuthenticationMethod(settings.ftpUsername, settings.ftpPassword);
                }
                else
                {
                    var key = settings.ftpPassphrase != string.Empty ? new PrivateKeyFile(settings.ftpKeyfile, settings.ftpPassphrase) : new PrivateKeyFile(settings.ftpKeyfile);
                    auth = new PrivateKeyAuthenticationMethod(settings.ftpUsername, key);
                }
                
                ftpConnectionInfo = new ConnectionInfo(
                    settings.ftpHost,
                    settings.ftpPort,
                    settings.ftpUsername,
                    auth);
            }
            catch (Exception)
            {
                ftpConnectionInfo = null;
            }
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

        public static bool ReadCommandLineArgs(IList<string> args)
        {
            if (args.Count == 0 || args == null)
                return true;

            if (args.Count > 1)
            {
                for (int i = 1; i < args.Count; i++)
                {
                    string extension = Path.GetExtension(args[i]);

                    XImage img = new XImage();
                    img.filename = Path.GetFileName(args[i]);
                    img.filepath = args[i];
                    string p = "dd.MM.yy HH:mm:ss";
                    img.datetime = DateTime.Now;
                    string d = DateTime.Now.ToString(p);
                    img.date = d;
                    img.anonupload = settings.anonUpload;
                    //if (extension != null && ImageExtensions.Contains(extension.ToLowerInvariant()))
                    if (extension != null && ImageFileTypes.SupportedTypes.Contains(extension.ToLowerInvariant()))
                    {
                        img.uploadsite = (UploadSite)settings.upload_site;
                        AddToQueue(img);
                    }
                    else if ((UploadSite)settings.fileUploadSite != UploadSite.None)
                    {
                        img.uploadsite = (UploadSite)settings.fileUploadSite;
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

        private static void AddToQueue(XImage x)
        {
            try
            {
                queue.Add(x);
                //totalUploading++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static bool TokenNeedsRefresh(UploadSite site)
        {
            switch (site)
            {
                case UploadSite.Imgur:
                    {
                        TimeSpan diff = DateTime.Now.Subtract(settings.lastRefreshTime);
                        //return diff.TotalSeconds >= settings.imgurTokenExpire;
                        return diff.TotalSeconds >= 3600; // BECAUSE IMGUR FAILS
                    }
                    
                case UploadSite.GoogleDrive:
                    {
                        TimeSpan diff = DateTime.Now.Subtract(settings.gdriveRefreshTime);
                        return diff.TotalSeconds >= settings.gdriveTokenExpire;
                    }
                default:
                    return true;

            }
        }

        private static dynamic StringToJson(string s)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<dynamic>(s);
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
                            var filesize = currentUpload.image?.Length ?? new FileInfo(currentUpload.filepath).Length;
                            //if (currentUpload.image == null)
                            if (filesize == 0)
                            {
                                Console.WriteLine(@"Tried to upload an empty file");
                                continue;
                            }
                            uploading++;
                            SetStatusBarText("Uploading.." + uploading + "/" + totalUploading);
                            Tuple<bool, string, string> result;
                            string response;
                            dynamic json;
                            switch (currentUpload.uploadsite)
                            {
                                case UploadSite.Imgur:
                                default:
                                    if (((filesize / 1024f) / 1024f) > 10)
                                    {
                                        totalUploading--;
                                        currentUpload.image = null;
                                        SetStatusBarText("File too large. Skipping.");
                                        continue;
                                    }
                                    // refresh imgur token if using account
                                    if (TokenNeedsRefresh(UploadSite.Imgur) && currentUpload.anonupload == false)
                                    {
                                        SetStatusBarText("Refreshing Imgur login..");
                                        ChangeTrayIcon("R");
                                        await OAuthHelpers.RefreshImgurToken();
                                    }
                                    response = await Uploader.HttpImgurUpload(currentUpload);
                                    json = StringToJson(response);
                                    string thumb = $"http://i.imgur.com/{json["data"]["id"]}m.jpg";

                                    result =  new Tuple<bool, string, string>(true, json["data"]["link"], thumb);
                                    break;
                                case UploadSite.Gyazo:
                                    if (settings.gyazoToken == string.Empty)
                                    {
                                        // login first
                                        MessageBox.Show("Login to Gyazo first!", "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                            MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                                        continue;
                                    }
                                    if (((filesize / 1024f) / 1024f) > 20)
                                    {
                                        totalUploading--;
                                        currentUpload.image = null;
                                        SetStatusBarText("File too large. Skipping.");
                                        continue;
                                    }
                                    response = await Uploader.HttpGyazoUpload(currentUpload);
                                    json = StringToJson(response);
                                    string link = json["url"];
                                    string thumbnail = json["thumb_url"];
                                    
                                    result = new Tuple<bool, string, string>(true, link, thumbnail);
                                    break;
                                case UploadSite.Puush:
                                    if (settings.puush_key == string.Empty)
                                    {
                                        MessageBox.Show("Puush api key required!", "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                            MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                                        continue;
                                    }
                                    if (((filesize / 1024f) / 1024f) > 20)
                                    {
                                        totalUploading--;
                                        currentUpload.image = null;
                                        SetStatusBarText("File too large. Skipping.");
                                        continue;
                                    }
                                    response = await Uploader.HttpPuushUpload(currentUpload);
                                    if (response.StartsWith("-"))
                                    {
                                        result = new Tuple<bool, string, string>(false, "", "");
                                        break;
                                    }
                                    //Console.WriteLine(response);
                                    string[] split = response.Split(',');
                                    string t = $"http://puush.me/{split[2]}";
                                    
                                    result =  new Tuple<bool, string, string>(true, split[1], t);
                                    break;
                                case UploadSite.Dropbox:
                                    if (settings.dropboxToken == string.Empty)
                                    {
                                        MessageBox.Show("Login to Dropbox first!", "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                            MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                                        continue;
                                    }
                                    if (((filesize / 1024f) / 1024f) > 150)
                                    {
                                        totalUploading--;
                                        currentUpload.image = null;
                                        SetStatusBarText("File too large. Skipping.");
                                        continue;
                                    }
                                    response = await Uploader.HttpDropboxUpload(currentUpload);
                                    json = StringToJson(response);
                                    var path = json["path_display"];
                                    response = await Uploader.GetDropboxSharedUrl(path);
                                    Console.WriteLine(path);
                                    result = new Tuple<bool, string, string>(true, response, "");
                                    break;
                                case UploadSite.GoogleDrive:
                                    if (settings.gdriveToken == string.Empty)
                                    {
                                        MessageBox.Show("Login to Google Drive first!", "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                            MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                                        continue;
                                    }
                                    if (((filesize / 1024f) / 1024f) > 150)
                                    {
                                        totalUploading--;
                                        currentUpload.image = null;
                                        SetStatusBarText("File too large. Skipping.");
                                        continue;
                                    }
                                    if (TokenNeedsRefresh(UploadSite.GoogleDrive))
                                    {
                                        SetStatusBarText("Refreshing GDrive login..");
                                        ChangeTrayIcon("R");
                                        await OAuthHelpers.RefreshGoogleDriveToken();
                                    }
                                    response = await Uploader.HttpGoogleDriveUpload(currentUpload);
                                    //https://drive.google.com/file/d/0B685Zhwu_twsc0R5Z0N6eVZZQ2M/view
                                    json = StringToJson(response);
                                    var id = json["id"];
                                    await Uploader.SetGoogleDriveFileShared(id);
                                    var url = $"https://drive.google.com/file/d/{id}/view";
                                    result = new Tuple<bool, string, string>(true, url, "");
                                    break;
                                case UploadSite.SFTP:
                                    if (settings.ftpProtocol == 0)
                                    {
                                        response = await Uploader.FTPUpload(currentUpload);
                                    }
                                    else
                                    {
                                        response = Uploader.SFTPUpload(currentUpload, ftpConnectionInfo);
                                    }
                                    
                                    result = new Tuple<bool, string, string>(true, response, "");
                                    break;
                            }

                            if (result.Item1)
                            {
                                uiContext.Post(x => AddXimageToList(currentUpload, result.Item2, result.Item3), null);
                                //AddXimageToList(currentUpload, result.Item2);
                                ChangeTrayIcon("F");
                            }
                            else
                            {
                                ChangeTrayIcon("E");
                                /*MessageBox.Show(result.Item2, "Something went wrong.", MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);*/
                                throw new Exception("Something went wrong.");
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

                            /*TaskDialog dialog = new TaskDialog();
                            dialog.Caption = "LXtory Error";
                            dialog.InstructionText = "LXtory Error";
                            dialog.Text = e.Message;
                            dialog.Icon = TaskDialogStandardIcon.Error;
                            dialog.Cancelable = false;
                            dialog.DetailsExpanded = false;
                            dialog.DetailsCollapsedLabel = "Show Stack Trace";
                            dialog.DetailsExpandedLabel = "Hide Stack Trace";
                            dialog.DetailsExpandedText = e.StackTrace;
                            dialog.Show();*/
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

        // Set some default settings values
        private static void SetDefaults()
        {
            settings.filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            /*settings.hkFullscreen = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F2));
            settings.hkCurrentwindow = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F3));
            settings.hkSelectedarea = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F4));
            settings.hkGifcapture = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F5));
            settings.hkD3DCap = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F6));*/
            settings.Save();
        }

        // Put some text in that statusbar
        private void SetStatusBarText(string s)
        {
            StatusText = s;
        }

        public ObservableCollection<XImage> Ximages
        {
            get { return ximages; }
            private set { SetProperty(ref ximages, value); }
        }

        public int ProgressValue
        {
            get { return progressValue; }
            private set { SetProperty(ref progressValue, value); }
        }

        public string StatusText
        {
            get { return statusText; }
            private set { SetProperty(ref statusText, value); }
        }

        private ImageSource icon;

        public ImageSource Icon
        {
            get { return icon; }
            set { SetProperty(ref icon, value); }
            /*set
            {
                if (value != icon)
                {
                    icon = value;
                    OnPropertyChanged("Icon"); 
                }
            }*/
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
                if (!settings.d3dAllScreens)
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

        // Capture whole (virtual)screen area
        public void CapFullscreen()
        {
            var top = SystemParameters.VirtualScreenTop;
            var left = SystemParameters.VirtualScreenLeft;
            var w = SystemParameters.VirtualScreenWidth;
            var h = SystemParameters.VirtualScreenHeight;
            ImageManager(EncodeImage(ScreenCap((int)w, (int)h, (int)left, (int)top)), "fullscreen");
        }

        // Capture current selected window
        public void CapWindow()
        {
            if (settings.d3dAutoDetect && NotificationState() == NativeMethods.USERNOTIFICATIONSTATE.QUNS_RUNNING_D3D_FULL_SCREEN)
            {
                //Console.WriteLine("D3DFullscreen Detected!");
                D3DCapPrimaryScreen();
                return;
            }

            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            NativeMethods.RECT rect = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(hWnd, ref rect);

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            var title = GetActiveWindowTitle();
            ImageManager(EncodeImage(ScreenCap(width, height, rect.Left, rect.Top)), title);
        }

        public void CapWindowFromPoint(int x, int y)
        {
            if (settings.d3dAutoDetect && NotificationState() == NativeMethods.USERNOTIFICATIONSTATE.QUNS_RUNNING_D3D_FULL_SCREEN)
            {
                //Console.WriteLine("D3DFullscreen Detected!");
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

            ImageManager(EncodeImage(ScreenCap(width, height, rect.Left, rect.Top)), title);
        }

        // Create an overlay, draw a rectangle on the overlay to cap that area
        public void CapArea()
        {
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
                            // get dpi multiplier
                            Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
                            
                            ImageManager(
                                EncodeImage(
                                    ScreenCap(Convert.ToInt32(notification.Rect.Width * m.M22), 
                                    Convert.ToInt32(notification.Rect.Height * m.M11),
                                    Convert.ToInt32(notification.Rect.X * m.M11), 
                                    Convert.ToInt32(notification.Rect.Y * m.M22))
                                    ), 
                                "areacapture");
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
                int x = notification.WindowLeft;
                int y = notification.WindowTop;
                int w = notification.WindowWidth;
                int h = notification.WindowHeight;
                int f = notification.GifFramerate;
                int d = notification.GifDuration;
                string datePattern = string.Empty != settings.dateTimeString ? settings.dateTimeString : defaultDateTimePattern;
                Gif gif = new Gif(f, d, w, h, x, y, datePattern);
                if (!notification.LoadCache)
                {
                    await gif.StartCapture();
                }
                else
                {
                    gif.LoadFromCache();
                }
                if (gif.Frames.Count > 0)
                {
                    bool cancelled = false;
                    if (settings.gifEditorEnabled)
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
                                img.filepath = Path.Combine(settings.filePath, filename);
                                string p = "dd.MM.yy HH:mm:ss";
                                img.datetime = DateTime.Now;
                                string date = DateTime.Now.ToString(p);
                                img.date = date;
                                img.uploadsite = (UploadSite)settings.upload_site;
                                if (settings.gifUpload)
                                {
                                    img.anonupload = settings.anonUpload;
                                    AddToQueue(img);
                                }
                                else
                                {
                                    //addXImageToList(img, "");
                                    AddXimageToList(img, "", "");
                                }
                            }
                        }
                    }
                }
            }
            gifCapturing = false;
        }

        // Capture an area of the screen, save as PNG
        private static Image ScreenCap(int width, int height, int rX, int rY)
        {
            // minimum size 1 pixel
            if (width < 1)
            {
                width = 1;
            }
            if (height < 1)
            {
                height = 1;
            }

            Image image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            using (var gfx = Graphics.FromImage(image))
            {
                gfx.CopyFromScreen(rX,
                    rY,
                    0,
                    0,
                    new System.Drawing.Size(width, height),
                    CopyPixelOperation.SourceCopy);
            }
            return image;
        }

        // Get active window title to be used in filenames
        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            var handle = NativeMethods.GetForegroundWindow();
            
            return NativeMethods.GetWindowText(handle, buff, nChars) > 0 ? buff.ToString() : "null";
        }

        // convert image into png encoded byte array
        private static byte[] EncodeImage(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        // Strip illegal characters from filename
        private static string MakeValidFileName(string name)
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
            if (!Directory.Exists(settings.filePath))
            {
                try
                {
                    Directory.CreateDirectory(settings.filePath);
                }
                catch (Exception e)
                {
                    StatusText = e.Message;
                    return string.Empty;
                }
            }
            string file = $"{filename}.png";
            string target = Path.Combine(settings.filePath, file);
            // if file with same name exists append a number to it
            while (File.Exists(target))
            {
                i++;
                file = $"{filename}({i}).png";
                target = Path.Combine(settings.filePath, file);
            }

            try
            {
                //bmp.Save(target,);
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
            //const string datePattern = @"dd-MM-yy_HH-mm-ss";
            string datePattern = string.Empty != settings.dateTimeString ? settings.dateTimeString : defaultDateTimePattern;
            string date = DateTime.Now.ToString(datePattern);
            string f = $"{filename}_{date}";

            XImage x = new XImage
            {
                image = image,
                filename = $"{f}.png",
                url = "",
                filepath = "",
                uploadsite = (UploadSite)settings.upload_site
            };
            const string p = "dd.MM.yy HH:mm:ss";
            x.datetime = DateTime.Now;
            string d = DateTime.Now.ToString(p);
            x.date = d;

            if (settings.saveLocal)
            {
                x.filepath = SaveImageToDisk(x.image, f);
                x.filename = Path.GetFileName(x.filepath);
                lock (ximages)
                {
                    ximages.Add(x);
                    WriteXML(); 
                }
            }

            if (settings.autoUpload)
            {
                x.anonupload = settings.anonUpload;
                AddToQueue(x);
            }
            else
            {
                x.image = null;
            }
        }

        // Add image to list
        private void AddXimageToList(XImage x, string url, string thumbnail)
        {
            if (x == null)
            {
                Console.WriteLine("ERROR");
                return;
            }
            if (!settings.saveLocal && x.filepath.Length == 0)
            {
                x.url = url;
                x.thumbnail = thumbnail;
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
                    y.thumbnail = thumbnail;
                    WriteXML();
                }
                else
                {
                    x.url = url;
                    x.thumbnail = thumbnail;
                    lock (ximages)
                    {
                        ximages.Add(x);
                        WriteXML(); 
                    }
                }
            }
            if (url.Length > 0)
            {
                if (settings.openInBrowser)
                {
                    Process.Start(url);
                }
                if (settings.lastToClipboard)
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
