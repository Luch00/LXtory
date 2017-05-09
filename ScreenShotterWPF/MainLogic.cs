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
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Renci.SshNet;
using Newtonsoft.Json;
using System.Dynamic;

namespace ScreenShotterWPF
{
    class MainLogic : BindableBase
    {
        private ObservableCollection<XImage> ximages = new ObservableCollection<XImage>();

        private static readonly BlockingCollection<XImage> queue = new BlockingCollection<XImage>();

        private static readonly Properties.Settings settings = Properties.Settings.Default;

        // For selecting window to capture
        private MouseKeyHook mHook;
        private readonly Action<bool> mouseAction;

        protected Thread clipboardThread;

        private bool overlay_created;

        private int progressValue;
        private string statusText;
        private bool cancelEnabled;

        private bool gifCapturing;
        
        private const string defaultDateTimePattern = @"dd-MM-yy_HH-mm-ss";

        private readonly SynchronizationContext uiContext;

        private CancellationTokenSource cancelUpload;

        private readonly bool windows8;
        
        public InteractionRequest<IConfirmation> OverlayRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifOverlayRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifEditorRequest { get; private set; }
        public InteractionRequest<IConfirmation> GifProgressRequest { get; private set; }

        public ICommand CancelCommand { get; private set; }

        private ConnectionInfo ftpConnectionInfo;

        public MainLogic()
        {
            windows8 = CheckIfWin8OrHigher();
            uiContext = SynchronizationContext.Current;
            //addXImageToList = AddXimageToList;
            mouseAction = HookMouseAction;
            Uploader.ProgressBarUpdate = ProgressAndIconChange;
            ClipboardMonitor.ClipboardEvent += new EventHandler(ClipboardChanged);
            BalloonMessage.ClipboardNotificationClicked += new EventHandler(ClipboardUpload);
            CreateSFTPConnectionInfo();
            OverlayRequest = new InteractionRequest<IConfirmation>();
            this.GifOverlayRequest = new InteractionRequest<IConfirmation>();
            this.GifEditorRequest = new InteractionRequest<IConfirmation>();
            this.GifProgressRequest = new InteractionRequest<IConfirmation>();
            this.CancelCommand = new DelegateCommand(CancelUpload);
            Ximages = ReadXML();
            CancelEnabled = false;
            if (settings.filePath == "")
            {
                SetDefaults();
            }
            StartUploads();
        }

        private static void ClipboardChanged(object sender, EventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                BalloonMessage.ClipboardNotification();
            }
            if (settings.clipboardFileDrop && Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                var ext = Path.GetExtension(files[0]);
                if (ImageFileTypes.SupportedTypes.Contains(ext))
                {
                    BalloonMessage.ClipboardNotification();
                }
            }
        }

        private static void ClipboardUpload(object sender, EventArgs e)
        {
            Image img = GetImageFromClipboard();
            if (img != null)
            {
                var x = CreateXImage(EncodeImage(img), "clipboard");
                img.Dispose();
                AddToQueue(x);
            }

            if (settings.clipboardFileDrop && Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                var ext = Path.GetExtension(files[0]);
                if (ImageFileTypes.SupportedTypes.Contains(ext))
                {
                    AddToQueue(CreateXImage(Path.GetFileName(files[0]), files[0]));
                }
            }
        }

        private static Image GetImageFromClipboard()
        {
            if (Clipboard.ContainsData("PNG"))
            {
                var png = Clipboard.GetData("PNG");
                if (png is MemoryStream)
                {
                    return Image.FromStream((MemoryStream)png);
                }
            }

            if (Clipboard.ContainsData(DataFormats.Dib))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (MemoryStream dib_stream = Clipboard.GetData(DataFormats.Dib) as MemoryStream)
                    {
                        PngBitmapEncoder enc = new PngBitmapEncoder();
                        enc.Interlace = PngInterlaceOption.Off;
                        enc.Frames.Add(DIBHelper.ImageFromClipboardDib(dib_stream));
                        enc.Save(ms);
                        return Image.FromStream(ms);
                    }
                }
            }

            if (Clipboard.ContainsImage())
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    PngBitmapEncoder enc = new PngBitmapEncoder();
                    enc.Interlace = PngInterlaceOption.Off;
                    enc.Frames.Add(BitmapFrame.Create(Clipboard.GetImage()));
                    enc.Save(ms);
                    return Image.FromStream(ms);
                }
            }

            return null;
        }

        private void CancelUpload()
        {
            cancelUpload?.Cancel();
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

        public static bool ReadCommandLineArgs(IList<string> args)
        {
            if (args.Count == 0 || args == null)
                return true;

            if (args.Count > 1)
            {
                for (int i = 1; i < args.Count; i++)
                {
                    var extension = Path.GetExtension(args[i]);
                    var img = CreateXImage(Path.GetFileName(args[i]), args[i]);
                    if (extension != null && ImageFileTypes.SupportedTypes.Contains(extension.ToLowerInvariant()))
                    {
                        img.Uploadsite = settings.imageUploadSite;
                        AddToQueue(img);
                    }
                    else if (settings.fileUploadSite != UploadSite.None)
                    {
                        img.IsImage = false;
                        img.Uploadsite = settings.fileUploadSite;
                        AddToQueue(img);
                    }
                }
            }

            return true;
        }

        private void StartUploads()
        {
            Task.Run(() => Upload());
            //var uploadTask = new Task(() => Upload());
            //uploadTask.Start();
            //await uploadTask;
        }

        public static void SetAsComplete()
        {
            queue.CompleteAdding();
        }

        public void RemoveXImage(XImage selected)
        {
            lock(ximages)
            {
                ximages.Remove(selected);
                WriteXML(ximages);
            }
        }

        private static void AddToQueue(XImage x)
        {
            try
            {
                queue.Add(x);
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

        private static bool CheckFileSizeLimit(long fileSize, int maxMB)
        {
            return ((fileSize / 1024L) / 1024L) > maxMB;
        }

        private async void Upload()
        {
            while (!queue.IsCompleted)
            {
                CancelEnabled = false;
                if (queue.TryTake(out XImage currentUpload, Timeout.Infinite))
                {
                    try
                    {
                        var filesize = currentUpload.image?.Length ?? new FileInfo(currentUpload.filepath).Length;
                        if (filesize == 0)
                        {
                            // Empty file, get next in queue
                            continue;
                        }

                        SetStatusBarText("Uploading..");
                        cancelUpload = new CancellationTokenSource();
                        CancelEnabled = true;
                        Tuple<string, string> result = null;
                        string response;
                        dynamic json;
                        switch (currentUpload.Uploadsite)
                        {
                            case UploadSite.Imgur:
                            default:
                                // check if filesize exceeds service limitations
                                if (CheckFileSizeLimit(filesize, settings.fileSizeImgur))
                                {
                                    currentUpload.image = null;
                                    SetStatusBarText("File too large. Skipping.");
                                    continue;
                                }

                                // refresh imgur token if using account
                                if (TokenNeedsRefresh(UploadSite.Imgur) && settings.anonUpload == false)
                                {
                                    SetStatusBarText("Refreshing Imgur login..");
                                    BalloonMessage.SetIcon("R");
                                    await OAuthHelpers.RefreshImgurToken();
                                    SetStatusBarText("Uploading..");
                                }

                                // do the upload
                                response = await Uploader.HttpImgurUpload(currentUpload, settings.anonUpload, cancelUpload.Token);

                                // parse response
                                json = JsonConvert.DeserializeObject<ExpandoObject>(response);
                                // add to album if specified
                                if (!settings.anonUpload)
                                {
                                    await Uploader.AddToImgurAlbum(json.data.id);
                                }

                                string thumb = $"http://i.imgur.com/{json.data.id}m.jpg";

                                result = new Tuple<string, string>(json.data.link, thumb);
                                break;
                            case UploadSite.Gyazo:
                                if (settings.gyazoToken == string.Empty)
                                {
                                    // login first
                                    MessageBox.Show("Login to Gyazo first!", "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                        MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                                    continue;
                                }

                                if (CheckFileSizeLimit(filesize, settings.fileSizeGyazo))
                                {
                                    currentUpload.image = null;
                                    SetStatusBarText("File too large. Skipping.");
                                    continue;
                                }
                                response = await Uploader.HttpGyazoUpload(currentUpload, cancelUpload.Token);

                                json = JsonConvert.DeserializeObject<ExpandoObject>(response);
                                string link = json.url;
                                string thumbnail = json.thumb_url;
                                    
                                result = new Tuple<string, string>(link, thumbnail);
                                break;
                            case UploadSite.Puush:
                                if (settings.puush_key == string.Empty)
                                {
                                    MessageBox.Show("Puush api key required!", "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                        MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                                    continue;
                                }

                                if(CheckFileSizeLimit(filesize, settings.fileSizePuush))
                                {
                                    currentUpload.image = null;
                                    SetStatusBarText("File too large. Skipping.");
                                    continue;
                                }
                                response = await Uploader.HttpPuushUpload(currentUpload, cancelUpload.Token);

                                string[] split = response.Split(',');
                                if (split.Length < 3)
                                {
                                    throw new Exception($"Puush error\r\n{response}");
                                }
                                string t = $"http://puush.me/{split[2]}";
                                    
                                result =  new Tuple<string, string>(split[1], t);
                                break;
                            case UploadSite.Dropbox:
                                if (settings.dropboxToken == string.Empty)
                                {
                                    MessageBox.Show("Login to Dropbox first!", "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                        MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                                    continue;
                                }
                                if (CheckFileSizeLimit(filesize, settings.fileSizeDropbox))
                                {
                                    currentUpload.image = null;
                                    SetStatusBarText("File too large. Skipping.");
                                    continue;
                                }
                                response = await Uploader.HttpDropboxUpload(currentUpload, cancelUpload.Token);

                                json = JsonConvert.DeserializeObject<ExpandoObject>(response);
                                var path = json.path_display;
                                response = await Uploader.GetDropboxSharedUrl(path);

                                result = new Tuple<string, string>(response, "");
                                break;
                            case UploadSite.GoogleDrive:
                                if (settings.gdriveToken == string.Empty)
                                {
                                    MessageBox.Show("Login to Google Drive first!", "LXtory Error", MessageBoxButton.OK, MessageBoxImage.Error,
                                        MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                                    continue;
                                }

                                if (CheckFileSizeLimit(filesize, settings.fileSizeGDrive))
                                {
                                    currentUpload.image = null;
                                    SetStatusBarText("File too large. Skipping.");
                                    continue;
                                }

                                if (TokenNeedsRefresh(UploadSite.GoogleDrive))
                                {
                                    SetStatusBarText("Refreshing GDrive login..");
                                    BalloonMessage.SetIcon("R");
                                    await OAuthHelpers.RefreshGoogleDriveToken();
                                    SetStatusBarText("Uploading..");
                                }

                                response = await Uploader.HttpGoogleDriveUpload(currentUpload, cancelUpload.Token);

                                json = JsonConvert.DeserializeObject<ExpandoObject>(response);
                                var id = json.id;
                                await Uploader.SetGoogleDriveFileShared(id, cancelUpload.Token);
                                var url = $"https://drive.google.com/file/d/{id}/view";

                                result = new Tuple<string, string>(url, "");
                                break;
                            case UploadSite.SFTP:
                                if (settings.ftpProtocol == 0)
                                {
                                    response = await Uploader.FTPUpload(currentUpload);
                                }
                                else
                                {
                                    response = Uploader.SFTPUpload(currentUpload, ftpConnectionInfo, cancelUpload.Token);
                                }

                                result = new Tuple<string, string>(response, "");
                                break;
                        }
                        uiContext.Post(x => AddXimageToList(currentUpload, result.Item1, result.Item2), null);
                        BalloonMessage.SetIcon("F");
                        BalloonMessage.ShowMessage("Upload complete");

                        if (queue.Count == 0)
                        {
                            SetStatusBarText("Done");
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is TaskCanceledException)
                        {
                            BalloonMessage.SetIcon("Default");
                            StatusText = "Upload task cancelled";
                            BalloonMessage.ShowMessage("Upload task cancelled", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                            continue;
                        }
                        BalloonMessage.SetIcon("E");

                        TaskDialog dialog = new TaskDialog()
                        {
                            Caption = "LXtory Error",
                            InstructionText = "LXtory Error",
                            Text = e.Message,
                            Icon = TaskDialogStandardIcon.Error,
                            Cancelable = false,
                            DetailsExpanded = false,
                            DetailsCollapsedLabel = "Show Stack Trace",
                            DetailsExpandedLabel = "Hide Stack Trace",
                            DetailsExpandedText = e.StackTrace
                        };
                        dialog.Show();
                    }
                }
            }
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

        public bool CancelEnabled
        {
            get { return cancelEnabled; }
            private set { SetProperty(ref cancelEnabled, value); }
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

        private void ProgressAndIconChange(int pctComplete)
        {
            ProgressValue = pctComplete;
            if (pctComplete >= 10 && pctComplete < 20)
            {
                BalloonMessage.SetIcon("10");
            }
            else if (pctComplete >= 20 && pctComplete < 30)
            {
                BalloonMessage.SetIcon("20");
            }
            else if (pctComplete >= 30 && pctComplete < 40)
            {
                BalloonMessage.SetIcon("30");
            }
            else if (pctComplete >= 40 && pctComplete < 50)
            {
                BalloonMessage.SetIcon("40");
            }
            else if (pctComplete >= 50 && pctComplete < 60)
            {
                BalloonMessage.SetIcon("50");
            }
            else if (pctComplete >= 60 && pctComplete < 70)
            {
                BalloonMessage.SetIcon("60");
            }
            else if (pctComplete >= 70 && pctComplete < 80)
            {
                BalloonMessage.SetIcon("70");
            }
            else if (pctComplete >= 80 && pctComplete < 90)
            {
                BalloonMessage.SetIcon("80");
            }
            else if (pctComplete >= 90)
            {
                BalloonMessage.SetIcon("90");
            }
        }

        private void HookMouseAction(bool b)
        {
            if (b)
            {
                NativeMethods.GetCursorPos(out NativeMethods.POINT p);
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
            var title = GetActiveWindowTitle();
            try
            {
                if (!settings.d3dAllScreens)
                {
                    ImageManager(DesktopDuplication.DuplicatePrimaryScreen(), title);
                }
                else
                {
                    ImageManager(DesktopDuplication.DuplicateAllScreens(), title);
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
                OverlayNotification notification = new OverlayNotification()
                {
                    Title = "Overlay",
                    WindowTop = SystemParameters.VirtualScreenTop,
                    WindowLeft = SystemParameters.VirtualScreenLeft,
                    WindowWidth = SystemParameters.VirtualScreenWidth,
                    WindowHeight = SystemParameters.VirtualScreenHeight
                };
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
            GifOverlayNotification notification = new GifOverlayNotification()
            {
                Title = "GifOverlay"
            };
            this.GifOverlayRequest.Raise(notification);
            if (notification != null && notification.Confirmed)
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
                        GifEditorNotification gen = new GifEditorNotification()
                        {
                            Title = "Gif Editor",
                            Gif = gif
                        };
                        this.GifEditorRequest.Raise(gen);
                        if (gen != null && !gen.Confirmed)
                        {
                            cancelled = true;
                        }
                    }
                    if (!cancelled)
                    {
                        GifProgressNotification gpn = new GifProgressNotification()
                        {
                            Title = "Encoding Gif..",
                            Gif = gif
                        };
                        this.GifProgressRequest.Raise(gpn);
                        if (gpn != null && gpn.Confirmed)
                        {
                            var filename = gpn.Name;
                            if (filename != string.Empty)
                            {
                                var img = CreateXImage(filename, Path.Combine(settings.filePath, filename));
                                if (settings.gifUpload)
                                {
                                    AddToQueue(img);
                                }
                                else
                                {
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
            
            return NativeMethods.GetWindowText(handle, buff, nChars) > 0 ? buff.ToString() : "image";
        }

        // convert image into png encoded byte array
        private static byte[] EncodeImage(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                image.Dispose();
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
                File.WriteAllBytes(target, bmp);
            }
            catch (Exception e)
            {
                StatusText = e.Message;
                return string.Empty;
            }

            return target;
        }

        private static XImage CreateXImage(byte[] image, string filename)
        {
            string datePattern = string.Empty != settings.dateTimeString ? settings.dateTimeString : defaultDateTimePattern;
            string date = DateTime.Now.ToString(datePattern);
            string f = $"{filename}_{date}";
            const string p = "dd.MM.yy HH:mm:ss";
            XImage x = new XImage
            {
                image = image,
                filename = $"{f}.png",
                url = "",
                filepath = "",
                datetime = DateTime.Now,
                date = DateTime.Now.ToString(p),
                Uploadsite = settings.imageUploadSite
            };
            return x;
        }

        private static XImage CreateXImage(string filename, string filepath)
        {
            const string p = "dd.MM.yy HH:mm:ss";
            XImage x = new XImage()
            {
                filename = filename,
                filepath = filepath,
                datetime = DateTime.Now,
                date = DateTime.Now.ToString(p),
                Uploadsite = settings.imageUploadSite
            };
            return x;
        }

        private void ImageManager(byte[] image, string filename)
        {
            var x = CreateXImage(image, filename);
            if (settings.saveLocal)
            {
                x.filepath = SaveImageToDisk(x.image, x.filename.Split('.').FirstOrDefault());
                x.filename = Path.GetFileName(x.filepath);
                lock (ximages)
                {
                    ximages.Add(x);
                    WriteXML(ximages);
                }
            }

            if (settings.autoUpload)
            {
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
                return;
            }
            if (!settings.saveLocal && x.filepath.Length == 0)
            {
                x.url = url;
                x.thumbnail = thumbnail;
                lock (ximages)
                {
                    ximages.Add(x);
                    WriteXML(ximages); 
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
                    WriteXML(ximages);
                }
                else
                {
                    x.url = url;
                    x.thumbnail = thumbnail;
                    lock (ximages)
                    {
                        ximages.Add(x);
                        WriteXML(ximages); 
                    }
                }
            }
            if (url.Length > 0 && x.IsImage)
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
            StatusText = "Clipboard copy failed.";
        }

        // Write history xml
        private static void WriteXML(ObservableCollection<XImage> ximages)
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
        private static ObservableCollection<XImage> ReadXML()
        {
            string f = Path.Combine((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)), @"Luch\LxTory\images.xml");
            XmlSerializer s = new XmlSerializer(typeof(ObservableCollection<XImage>));
            if (File.Exists(f))
            {
                using (FileStream fs = new FileStream(f, FileMode.Open))
                {
                    var ximages = (ObservableCollection<XImage>)s.Deserialize(fs);
                    fs.Close();
                    return ximages;
                }
            }
            return new ObservableCollection<XImage>();
        }

        private static NativeMethods.USERNOTIFICATIONSTATE NotificationState()
        {
            var value = NativeMethods.SHQueryUserNotificationState(out NativeMethods.USERNOTIFICATIONSTATE state);
            return state;
        }
    }
}
