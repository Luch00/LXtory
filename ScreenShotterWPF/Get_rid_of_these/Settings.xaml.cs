//using Microsoft.Win32;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using System.Reflection;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Input;

//namespace ScreenShotterWPF
//{
//    /// <summary>
//    /// Interaction logic for Settings.xaml
//    /// </summary>
//    public partial class Settings : INotifyPropertyChanged
//    {
//        // TODO CHANGE EVERYTHING TO BINDINGS
//        private string accesscode = string.Empty;

//        private const string CloseWindowResponse = "<!DOCTYPE html><html><head></head><body style=\"background-color: #121211; font-family: Arial,sans-serif;    color: rgb(221, 221, 209);\"><h1>Authorization Successfull</h1><p>You can now close this window</p></body></html>";

//        private bool fullscreenCtrl;
//        private bool fullscreenShift;
//        private bool fullscreenAlt;
//        private int fullscreenKey;
//        private string fullscreenString;

//        private bool currentwindowCtrl;
//        private bool currentwindowShift;
//        private bool currentwindowAlt;
//        private int currentwindowKey;
//        private string currentwindowString;

//        private bool selectedareaCtrl;
//        private bool selectedareaShift;
//        private bool selectedareaAlt;
//        private int selectedareaKey;
//        private string selectedareaString;

//        private bool d3dCtrl;
//        private bool d3dShift;
//        private bool d3dAlt;
//        private int d3dKey;
//        private string d3dString;

//        public event PropertyChangedEventHandler PropertyChanged;

//        public Settings()
//        {
//            InitializeComponent();
//            this.DataContext = this;
//            settingsBrowse.IsEnabled = true;
//            setValues();
//        }

//        private void RaisePropertyChanged(string propertyName)
//        {
//            PropertyChangedEventHandler handler = PropertyChanged;
//            if (handler != null)
//            {
//                handler(this, new PropertyChangedEventArgs(propertyName));
//            }
//        }

//        public bool FullscreenCtrl
//        {
//            set { fullscreenCtrl = value; RaisePropertyChanged("FullscreenCtrl"); }
//            get { return fullscreenCtrl; }
//        }

//        public bool FullscreenShift
//        {
//            get { return fullscreenShift; }
//            set { fullscreenShift = value; RaisePropertyChanged("FullscreenShift"); }
//        }

//        public bool FullscreenAlt
//        {
//            get { return fullscreenAlt; }
//            set { fullscreenAlt = value; RaisePropertyChanged("FullscreenAlt"); }
//        }

//        public bool CurrentwindowCtrl
//        {
//            get { return currentwindowCtrl; }
//            set { currentwindowCtrl = value; RaisePropertyChanged("CurrentwindowCtrl"); }
//        }

//        public bool CurrentwindowShift
//        {
//            get { return currentwindowShift; }
//            set { currentwindowShift = value; RaisePropertyChanged("CurrentwindowShift"); }
//        }

//        public bool CurrentwindowAlt
//        {
//            get { return currentwindowAlt; }
//            set { currentwindowAlt = value; RaisePropertyChanged("CurrentwindowAlt"); }
//        }

//        public bool SelectedareaCtrl
//        {
//            get { return selectedareaCtrl; }
//            set { selectedareaCtrl = value; RaisePropertyChanged("SelectedareaCtrl"); }
//        }

//        public bool SelectedareaShift
//        {
//            get { return selectedareaShift; }
//            set { selectedareaShift = value; RaisePropertyChanged("SelectedareaShift"); }
//        }

//        public bool SelectedareaAlt
//        {
//            get { return selectedareaAlt; }
//            set { selectedareaAlt = value; RaisePropertyChanged("SelectedareaAlt"); }
//        }

//        public bool D3dCtrl
//        {
//            get { return d3dCtrl; }
//            set { d3dCtrl = value; RaisePropertyChanged("D3dCtrl"); }
//        }

//        public bool D3dShift
//        {
//            get { return d3dShift; }
//            set { d3dShift = value; RaisePropertyChanged("D3dShift"); }
//        }

//        public bool D3dAlt
//        {
//            get { return d3dAlt; }
//            set { d3dAlt = value; RaisePropertyChanged("D3dAlt"); }
//        }

//        public int FullscreenKey
//        {
//            get { return fullscreenKey; }
//            set { fullscreenKey = value; FullscreenString = KeyInterop.KeyFromVirtualKey(fullscreenKey).ToString(); }
//        }

//        public int CurrentwindowKey
//        {
//            get { return currentwindowKey; }
//            set { currentwindowKey = value; CurrentwindowString = KeyInterop.KeyFromVirtualKey(currentwindowKey).ToString(); }
//        }

//        public int SelectedareaKey
//        {
//            get { return selectedareaKey; }
//            set { selectedareaKey = value; SelectedareaString = KeyInterop.KeyFromVirtualKey(selectedareaKey).ToString(); }
//        }

//        public int D3dKey
//        {
//            get { return d3dKey; }
//            set { d3dKey = value; D3dString = KeyInterop.KeyFromVirtualKey(d3dKey).ToString(); }
//        }

//        public string FullscreenString
//        {
//            get { return fullscreenString; }
//            set { fullscreenString = value; RaisePropertyChanged("FullscreenString"); }
//        }

//        public string CurrentwindowString
//        {
//            get { return currentwindowString; }
//            set { currentwindowString = value; RaisePropertyChanged("CurrentwindowString"); }
//        }

//        public string SelectedareaString
//        {
//            get { return selectedareaString; }
//            set { selectedareaString = value; RaisePropertyChanged("SelectedareaString"); }
//        }

//        public string D3dString
//        {
//            get { return d3dString; }
//            set { d3dString = value; RaisePropertyChanged("D3dString"); }
//        }

//        private void setValues()
//        {
//            textFilepath.Text = Properties.Settings.Default.filePath;
//            checkBoxLocal.IsChecked = Properties.Settings.Default.saveLocal;
//            checkUpload.IsChecked = Properties.Settings.Default.autoUpload;
//            checkStartMinimized.IsChecked = Properties.Settings.Default.startMinimized;
//            checkMinimizeTray.IsChecked = Properties.Settings.Default.minimizeToTray;
//            checkOpeninBrowser.IsChecked = Properties.Settings.Default.openInBrowser;
//            checkClosetoTray.IsChecked = Properties.Settings.Default.closeToTray;
//            checkRunatStart.IsChecked = Properties.Settings.Default.runAtStart;
//            checkCopytoClipboard.IsChecked = Properties.Settings.Default.lastToClipboard;

//            checkGifUpload.IsChecked = Properties.Settings.Default.gifUpload;
//            checkGifEditor.IsChecked = Properties.Settings.Default.gifEditorEnabled;
//            checkGifCaptureCursor.IsChecked = Properties.Settings.Default.gifCaptureCursor;
//            gifFrameRate.Value = Properties.Settings.Default.gifFrameRate;
//            gifDuration.Value = Properties.Settings.Default.gifDuration;
//            //gifQuality.Value = Properties.Settings.Default.gifQuality;

//            detectExclusive.IsChecked = Properties.Settings.Default.d3dAutoDetect;
//            fullscreenD3D.IsChecked = Properties.Settings.Default.d3dAllScreens;

//            radioAnon.IsChecked = Properties.Settings.Default.anonUpload;
//            radioAccount.IsChecked = !Properties.Settings.Default.anonUpload;
//            switch (Properties.Settings.Default.upload_site)
//            {
//                case 1:
//                    radioGyazo.IsChecked = true;
//                    break;
//                case 2:
//                    radioPuush.IsChecked = true;
//                    break;
//                default:
//                    radioImgur.IsChecked = true;
//                    break;
//            }

//            txtPuushApiKey.Text = Properties.Settings.Default.puush_key;
//            SetHotkeys();
//            if (Properties.Settings.Default.username != "")
//            {
//                labelUsername.Content = Properties.Settings.Default.username;
//                btnLogin.Content = "Logout";
//            }
//            else
//            {
//                labelUsername.Content = "(Not logged in)";
//            }

//            if (Properties.Settings.Default.shellExtActive)
//            {
//                btnRegister.Content = "Unregister";
//            }
//            btnRegister.IsEnabled = true;
//        }

//        private void SetHotkeys()
//        {
//            if (Properties.Settings.Default.hkFullscreen != null)
//            {
//                FullscreenCtrl = Properties.Settings.Default.hkFullscreen.ctrl;
//                FullscreenShift = Properties.Settings.Default.hkFullscreen.shift;
//                FullscreenAlt = Properties.Settings.Default.hkFullscreen.alt;
//                FullscreenKey = Properties.Settings.Default.hkFullscreen.vkKey;
//            }
//            else
//            {
//                Properties.Settings.Default.hkFullscreen = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F2));
//                FullscreenCtrl = Properties.Settings.Default.hkFullscreen.ctrl;
//                FullscreenShift = Properties.Settings.Default.hkFullscreen.shift;
//                FullscreenAlt = Properties.Settings.Default.hkFullscreen.alt;
//                FullscreenKey = Properties.Settings.Default.hkFullscreen.vkKey;
//            }
//            if (Properties.Settings.Default.hkCurrentwindow != null)
//            {
//                CurrentwindowCtrl = Properties.Settings.Default.hkCurrentwindow.ctrl;
//                CurrentwindowShift = Properties.Settings.Default.hkCurrentwindow.shift;
//                CurrentwindowAlt = Properties.Settings.Default.hkCurrentwindow.alt;
//                CurrentwindowKey = Properties.Settings.Default.hkCurrentwindow.vkKey;
//            }
//            else
//            {
//                Properties.Settings.Default.hkCurrentwindow = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F3));
//                FullscreenCtrl = Properties.Settings.Default.hkFullscreen.ctrl;
//                FullscreenShift = Properties.Settings.Default.hkFullscreen.shift;
//                FullscreenAlt = Properties.Settings.Default.hkFullscreen.alt;
//                FullscreenKey = Properties.Settings.Default.hkFullscreen.vkKey;
//            }
//            if (Properties.Settings.Default.hkSelectedarea != null)
//            {
//                SelectedareaCtrl = Properties.Settings.Default.hkSelectedarea.ctrl;
//                SelectedareaShift = Properties.Settings.Default.hkSelectedarea.shift;
//                SelectedareaAlt = Properties.Settings.Default.hkSelectedarea.alt;
//                SelectedareaKey = Properties.Settings.Default.hkSelectedarea.vkKey;
//            }
//            else
//            {
//                Properties.Settings.Default.hkSelectedarea = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F4));
//                FullscreenCtrl = Properties.Settings.Default.hkFullscreen.ctrl;
//                FullscreenShift = Properties.Settings.Default.hkFullscreen.shift;
//                FullscreenAlt = Properties.Settings.Default.hkFullscreen.alt;
//                FullscreenKey = Properties.Settings.Default.hkFullscreen.vkKey;
//            }
//            if (Properties.Settings.Default.hkD3DCap != null)
//            {
//                D3dCtrl = Properties.Settings.Default.hkD3DCap.ctrl;
//                D3dShift = Properties.Settings.Default.hkD3DCap.shift;
//                D3dAlt = Properties.Settings.Default.hkD3DCap.alt;
//                D3dKey = Properties.Settings.Default.hkD3DCap.vkKey;
//            }
//            else
//            {
//                Properties.Settings.Default.hkD3DCap = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F5));
//                FullscreenCtrl = Properties.Settings.Default.hkFullscreen.ctrl;
//                FullscreenShift = Properties.Settings.Default.hkFullscreen.shift;
//                FullscreenAlt = Properties.Settings.Default.hkFullscreen.alt;
//                FullscreenKey = Properties.Settings.Default.hkFullscreen.vkKey;
//            }
//        }

//        /*private static bool CheckAdmin()
//        {
//            try
//            {
//                WindowsIdentity user = WindowsIdentity.GetCurrent();
//                WindowsPrincipal principal = new WindowsPrincipal(user);
//                return principal.IsInRole(WindowsBuiltInRole.Administrator);
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }*/

//        private void Button_Click(object sender, RoutedEventArgs e)
//        {
//            if (radioPuush.IsChecked.Value && txtPuushApiKey.Text.Length < 1)
//            {
//                lblStatus.Content = "Enter Puush API Key";
//                return;
//            }
//            if (fullscreenKey != 0)
//            {
//                Properties.Settings.Default.hkFullscreen = new HotKey(fullscreenCtrl, fullscreenAlt, fullscreenShift, fullscreenKey); 
//            }
//            if (currentwindowKey != 0)
//            {
//                Properties.Settings.Default.hkCurrentwindow = new HotKey(currentwindowCtrl, currentwindowAlt, currentwindowShift, currentwindowKey); 
//            }
//            if (selectedareaKey != 0)
//            {
//                Properties.Settings.Default.hkSelectedarea = new HotKey(selectedareaCtrl, selectedareaAlt, selectedareaShift, selectedareaKey); 
//            }
//            if (d3dKey != 0)
//            {
//                Properties.Settings.Default.hkD3DCap = new HotKey(d3dCtrl, d3dAlt, d3dShift, d3dKey);
//            }
//            if (radioImgur.IsChecked.Value)
//            {
//                Properties.Settings.Default.upload_site = 0;
//            }
//            else if (radioGyazo.IsChecked.Value)
//            {
//                Properties.Settings.Default.upload_site = 1;
//            }
//            else if (radioPuush.IsChecked.Value)
//            {
//                Properties.Settings.Default.upload_site = 2;
//            }

//            SetStartUp(checkRunatStart.IsChecked.Value);

//            Properties.Settings.Default.lastToClipboard = checkCopytoClipboard.IsChecked.Value;
//            Properties.Settings.Default.minimizeToTray = checkMinimizeTray.IsChecked.Value;
//            Properties.Settings.Default.openInBrowser = checkOpeninBrowser.IsChecked.Value;
//            Properties.Settings.Default.runAtStart = checkRunatStart.IsChecked.Value;
//            Properties.Settings.Default.saveLocal = checkBoxLocal.IsChecked.Value;
//            Properties.Settings.Default.anonUpload = radioAnon.IsChecked.Value;
//            Properties.Settings.Default.autoUpload = checkUpload.IsChecked.Value;
//            Properties.Settings.Default.closeToTray = checkClosetoTray.IsChecked.Value;
//            Properties.Settings.Default.startMinimized = checkStartMinimized.IsChecked.Value;

//            Properties.Settings.Default.gifUpload = checkGifUpload.IsChecked.Value;
//            Properties.Settings.Default.gifEditorEnabled = checkGifEditor.IsChecked.Value;
//            Properties.Settings.Default.gifCaptureCursor = checkGifCaptureCursor.IsChecked.Value;
//            Properties.Settings.Default.gifFrameRate = gifFrameRate?.Value ?? 15;
//            Properties.Settings.Default.gifDuration = gifDuration?.Value ?? 5;
//            //Properties.Settings.Default.gifQuality = gifQuality?.Value ?? 5;

//            Properties.Settings.Default.d3dAllScreens = fullscreenD3D.IsChecked.Value;
//            Properties.Settings.Default.d3dAutoDetect = detectExclusive.IsChecked.Value;

//            Properties.Settings.Default.puush_key = txtPuushApiKey.Text;
//            if (Directory.Exists(textFilepath.Text))
//            {
//                Properties.Settings.Default.filePath = textFilepath.Text;
//            }
//            else
//            {
//                Properties.Settings.Default.filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
//            }

//            DialogResult = true;
//        }

//        private static void SetStartUp(bool s)
//        {
//            using(RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
//            {
//                if (s)
//                {
//                    if (rk.GetValue("LXtory") == null)
//                    {
//                        rk.SetValue("LXtory", Assembly.GetExecutingAssembly().Location);
//                    }                    
//                }
//                else
//                {
//                    if (rk.GetValue("LXtory") != null)
//                    {
//                        rk.DeleteValue("LXtory");
//                    }
//                }
//            }
//        }

//        private async void btnLogin_Click(object sender, RoutedEventArgs e)
//        {
//            if (Properties.Settings.Default.username == string.Empty && Properties.Settings.Default.accessToken == string.Empty)
//            {
//                lblStatus.Content = "Waiting for Authorization..";
//                authProgress.Visibility = Visibility.Visible;
//                authProgress.IsIndeterminate = true;
//                btnLogin.IsEnabled = false;
//                try
//                {
//                    string authCode = await GetAuthCode();
//                    if (authCode != string.Empty)
//                    {
//                        //get tokens
//                        await Imgur.GetToken(authCode);
//                        labelUsername.Content = Properties.Settings.Default.username;
//                        lblStatus.Content = "Authorization complete";
//                        btnLogin.Content = "Logout";
//                        btnLogin.IsEnabled = true;
//                        authProgress.Visibility = Visibility.Hidden;
//                        authProgress.IsIndeterminate = false;
//                    }
//                    else
//                    {
//                        // auth failed
//                        authProgress.Visibility = Visibility.Hidden;
//                        authProgress.IsIndeterminate = false;
//                        btnLogin.IsEnabled = true;
//                        lblStatus.Content = "Authorization failed";
//                    }
//                }
//                catch (Exception)
//                {
//                    authProgress.Visibility = Visibility.Hidden;
//                    authProgress.IsIndeterminate = false;
//                    btnLogin.IsEnabled = true;
//                    lblStatus.Content = "Authorization failed";
//                    //throw;
//                }
//            }
//            else
//            {
//                Properties.Settings.Default.accessToken = "";
//                Properties.Settings.Default.refreshToken = "";
//                Properties.Settings.Default.username = "";
//                Properties.Settings.Default.Save();
//                btnLogin.Content = "Login";
//                labelUsername.Content = "Not logged in";
//            }
//        }

//        private async Task<string> GetAuthCode()
//        {
//            //IPAddress local = IPAddress.Loopback;
//            TcpListener listener = new TcpListener(IPAddress.Loopback, 8080);
//            listener.Start();
//            Byte[] bytes = new Byte[256];
//            bool receiving = true;
//            do
//            {
//                Console.WriteLine(@"WAITING CONNECTION");
//                Process.Start("https://api.imgur.com/oauth2/authorize?client_id=83c1c8bf9f4d2b1&response_type=code&state=LXtory");
//                TcpClient client = await listener.AcceptTcpClientAsync();
//                Console.WriteLine(@"CONNECTED");
//                NetworkStream stream = client.GetStream();
//                int i;
//                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0 && receiving)
//                {
//                    var data = Encoding.ASCII.GetString(bytes, 0, i);
//                    if (data.StartsWith("GET /LXtory_Auth/"))
//                    {
//                        receiving = false;
//                        var regex = new Regex(@"code=(.*?) ");
//                        var result = regex.Match(data);
//                        if (result.Success)
//                        {
//                            accesscode = result.Groups[1].Value;
//                            byte[] msg = Encoding.ASCII.GetBytes(CloseWindowResponse);
//                            stream.Write(msg, 0, msg.Length);
//                        }
//                    }
//                    //Console.WriteLine("Received: {0}", data);
//                    //data = data.ToUpper();

//                    /*byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
//                        stream.Write(msg, 0, msg.Length);
//                        Console.WriteLine("Sent: {0}", data);*/
//                }
//                client.Close();
//            } while (receiving);

//            /*System.Net.HttpListener listener = new System.Net.HttpListener();
//                listener.Prefixes.Add("http://localhost:80/");
//                listener.Start();
//                Process.Start("https://api.imgur.com/oauth2/authorize?client_id=83c1c8bf9f4d2b1&response_type=code&state=LXtory");
//                HttpListenerContext context = await listener.GetContextAsync();
//                HttpListenerRequest request = context.Request;

//                if (request.Url.AbsolutePath == "/LXtory_Auth/")
//                {
//                    accesscode = request.Url.Query.Substring(request.Url.Query.LastIndexOf('=') + 1);
//                }
//                HttpListenerResponse response = context.Response;
//                byte[] buffer = Encoding.UTF8.GetBytes(CloseWindowResponse);
//                response.ContentLength64 = buffer.Length;
//                Stream output = response.OutputStream;
//                output.Write(buffer, 0, buffer.Length);
//                output.Close();
//                listener.Stop();*/
//            listener.Stop();
//            return accesscode;
//        }

//        private static void AddContextMenuItems()
//        {
//            string executablePath = Assembly.GetExecutingAssembly().Location;
//            List<string> keys = new List<string> {
//                "SOFTWARE\\Classes\\giffile\\shell\\LXtory",
//                "SOFTWARE\\Classes\\jpegfile\\shell\\LXtory",
//                "SOFTWARE\\Classes\\pngfile\\shell\\LXtory",
//                "SOFTWARE\\Classes\\SystemFileAssociations\\image\\shell\\LXtory" };

//            foreach (string k in keys)
//            {
//                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(k))
//                {
//                    if (key != null)
//                    {
//                        key.SetValue("", "Upload With LXtory");
//                        key.SetValue("Icon", $"\"{executablePath}\"");
//                        RegistryKey subkey = key.CreateSubKey("command");
//                        if (subkey != null)
//                        {
//                            subkey.SetValue("", $"\"{executablePath}\" \"%1\"");
//                            subkey.Close();
//                        }
//                    }
//                }
//            }
//        }

//        private static void RemoveContextMenuItems()
//        {
//            List<string> keys = new List<string> {
//                "SOFTWARE\\Classes\\giffile\\shell\\LXtory",
//                "SOFTWARE\\Classes\\jpegfile\\shell\\LXtory",
//                "SOFTWARE\\Classes\\pngfile\\shell\\LXtory",
//                "SOFTWARE\\Classes\\SystemFileAssociations\\image\\shell\\LXtory" };

//            foreach (string key in keys)
//            {
//                Registry.CurrentUser.DeleteSubKeyTree(key);
//            }
//        }

//        /*private async Task<string> GetAuthCode(int site)
//        {
//            try
//            {
//                string code = string.Empty;

//                Griffin.Net.Protocols.Http.HttpListener listener = new Griffin.Net.Protocols.Http.HttpListener();
//                listener.MessageReceived = OnHttpMessage;
//                var timeout = new TimeSpan(0, 0, 50);
//                var start = DateTime.Now;
//                listener.Start(System.Net.IPAddress.Loopback, 80);
//                receiving = true;
//                if (site == 0)
//                {
//                    Process.Start("https://api.imgur.com/oauth2/authorize?client_id=83c1c8bf9f4d2b1&response_type=code&state=LXtory");
//                }
//                else if (site == 1)
//                {
//                    //Process.Start("https://api.gyazo.com/oauth/authorize?client_id=cf9b8d3161a90fe6d508191d6e96e8b1193114a32011c2787d329405fa767925&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%2FLXtory_Auth%2F");
//                }

//                while (receiving)
//                {
//                    var taskTimeout = Task.Delay(timeout);
//                    var taskCheck = Task.Delay(200);
//                    await Task.WhenAny(taskCheck, taskTimeout);

//                    if (taskCheck.IsCompleted)
//                    {
//                        if (receiving)
//                        {
//                            timeout = timeout.Subtract(new TimeSpan(0, 0, 0, 0, 200));
//                        }
//                        else
//                        {
//                            break;
//                        }
//                    }
//                    else if(taskTimeout.IsCompleted)
//                    {
//                        receiving = false;
//                        break;
//                    }
//                }
//                return accesscode;
//            }
//            catch (Exception e)
//            {
//                MessageBox.Show(e.Message + " \n" + e.StackTrace);
//                return string.Empty;
//            }
//        }*/

//        /*private void OnHttpMessage(ITcpChannel channel, object message)
//        {
//            try
//            {   
//                var request = (Griffin.Net.Protocols.Http.HttpRequestBase)message;
//                var response = request.CreateResponse();
                
//                if (request.Uri.AbsolutePath == "/LXtory_Auth/")
//                {
//                    accesscode = request.Uri.Query.Substring(request.Uri.Query.LastIndexOf('=') + 1);
//                    //accesscode = request.Uri.Query;
//                    var body = Encoding.UTF8.GetBytes(CloseWindowResponse);
//                    response.Body = new MemoryStream(body);
//                    response.ContentType = "text/html";
//                    channel.Send(response);
//                    receiving = false;
//                    return;
//                }
//                channel.Close();
//            }
//            catch (Exception)
//            {
//            }
//        }*/

//        private void btnRegister_Click(object sender, RoutedEventArgs e)
//        {
//            if (Properties.Settings.Default.shellExtActive)
//            {
//                RemoveContextMenuItems();
//                Properties.Settings.Default.shellExtActive = false;
//                btnRegister.Content = "Register";
//            }
//            else
//            {
//                AddContextMenuItems();
//                Properties.Settings.Default.shellExtActive = true;
//                btnRegister.Content = "Unregister";
//            }
//        }

//        private void textFullscreen_PreviewKeyDown(object sender, KeyEventArgs e)
//        {
//            e.Handled = true;
//        }

//        private void textCurrentWindow_PreviewKeyDown(object sender, KeyEventArgs e)
//        {
//            e.Handled = true;
//        }

//        private void textSelectedArea_PreviewKeyDown(object sender, KeyEventArgs e)
//        {
//            e.Handled = true;
//        }

//        private void textD3DCapture_PreviewKeyDown(object sender, KeyEventArgs e)
//        {
//            e.Handled = true;
//        }

//        private void textCurrentWindow_PreviewKeyUp(object sender, KeyEventArgs e)
//        {
//            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
//            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
//                return;

//            CurrentwindowKey = KeyInterop.VirtualKeyFromKey(key);
//            e.Handled = true;
//        }

//        private void textFullscreen_PreviewKeyUp(object sender, KeyEventArgs e)
//        {
//            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
//            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
//                return;

//            FullscreenKey = KeyInterop.VirtualKeyFromKey(key);
//            e.Handled = true;
//        }

//        private void textSelectedArea_PreviewKeyUp(object sender, KeyEventArgs e)
//        {
//            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
//            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
//                return;

//            SelectedareaKey = KeyInterop.VirtualKeyFromKey(key);
//            e.Handled = true;
//        }

//        private void textD3DCapture_PreviewKeyUp(object sender, KeyEventArgs e)
//        {
//            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
//            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
//                return;

//            D3dKey = KeyInterop.VirtualKeyFromKey(key);
//            e.Handled = true;
//        }

//        private void checkBoxLocal_Unchecked(object sender, RoutedEventArgs e)
//        {
//            if (checkUpload.IsChecked == false)
//            {
//                checkUpload.IsChecked = true;
//            }
//        }

//        private void checkUpload_Unchecked(object sender, RoutedEventArgs e)
//        {
//            if (checkBoxLocal.IsChecked == false)
//            {
//                checkBoxLocal.IsChecked = true;
//            }
//        }
//    }
//}
