using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ScreenShotterWPF.Notifications;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using Prism.Commands;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ScreenShotterWPF.ViewModels
{
    public class SettingsViewModel : BindableBase, IInteractionRequestAware
    {
        private SettingsNotification notification;

        public Action FinishInteraction { get; set; }
        
        private string accesscode = string.Empty;

        private const string CloseWindowResponse = "<!DOCTYPE html><html><head></head><body style=\"background-color: #121211; font-family: Arial,sans-serif;    color: rgb(221, 221, 209);\"><h1>Authorization Successfull</h1><p>You can now close this window</p></body></html>";

        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand BrowseCommand { get; private set; }
        public ICommand LoginCommand { get; private set; }
        public ICommand LoginCommandGyazo { get; private set; }
        public ICommand RegisterCommand { get; private set; }

        private bool fullscreenCtrl;
        private bool fullscreenShift;
        private bool fullscreenAlt;
        private int fullscreenKey;
        private string fullscreenString;

        private bool currentwindowCtrl;
        private bool currentwindowShift;
        private bool currentwindowAlt;
        private int currentwindowKey;
        private string currentwindowString;

        private bool selectedareaCtrl;
        private bool selectedareaShift;
        private bool selectedareaAlt;
        private int selectedareaKey;
        private string selectedareaString;

        private bool gifcaptureCtrl;
        private bool gifcaptureShift;
        private bool gifcaptureAlt;
        private int gifcaptureKey;
        private string gifcaptureString;

        private bool d3dCtrl;
        private bool d3dShift;
        private bool d3dAlt;
        private int d3dKey;
        private string d3dString;

        private string textFilepath;
        private bool localEnabled;
        private bool uploadEnabled;
        private bool startMinimized;
        private bool minimizeToTray;
        private bool openInBrowser;
        private bool closetToTray;
        private bool runAtStart;
        private bool copyToClipboard;
        private bool gifUpload;
        private bool gifEditor;
        private bool gifCaptureCursor;
        private int gifFramerate;
        private int gifDuration;
        private bool detectExclusive;
        private bool fullscreenD3D;
        private string puushApiKey;
        private string username;
        private string loginButtonText;
        private string loginButtonTextGyazo;
        private string registerButtonText;
        private bool registerEnabled;
        private string statusLabelText;
        private Visibility authProgressVisibility;
        private bool loginEnabled;
        private bool loginEnabledGyazo;
        private bool anonUpload;
        private int uploadValue;
        private string dateTimeString;

        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is SettingsNotification)
                {
                    // To keep the code simple, this is the only property where we are raising the PropertyChanged event,
                    // as it's required to update the bindings when this property is populated.
                    // Usually you would want to raise this event for other properties too.
                    this.notification = value as SettingsNotification;
                    SetValues();
                    this.OnPropertyChanged(() => this.Notification);
                }
            }
        }

        public SettingsViewModel()
        {
            //settingsBrowse.IsEnabled = true;
            this.ConfirmCommand = new DelegateCommand(Confirm);
            this.CancelCommand = new DelegateCommand(Cancel);
            this.BrowseCommand = new DelegateCommand(Browse);
            this.LoginCommand = new DelegateCommand(Login);
            this.LoginCommandGyazo = new DelegateCommand(GyazoLogin);
            this.RegisterCommand = new DelegateCommand(Register);
            AuthProgressVisibility = Visibility.Hidden;
        }

        private void Browse()
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Folder...",
                IsFolderPicker = true,
                InitialDirectory = this.textFilepath,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = dialog.FileName;
                TextFilepath = folder;
            }
        }

        #region KeyHandlers

        public void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        public void PreviewKeyUp_Fullscreen(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
                return;

            FullscreenKey = KeyInterop.VirtualKeyFromKey(key);
            e.Handled = true;
        }

        public void PreviewKeyUp_Currentwindow(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
                return;

            CurrentwindowKey = KeyInterop.VirtualKeyFromKey(key);
            e.Handled = true;
        }

        public void PreviewKeyUp_Selectedarea(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
                return;

            SelectedareaKey = KeyInterop.VirtualKeyFromKey(key);
            e.Handled = true;
        }

        public void PreviewKeyUp_Gifcapture(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
                return;

            GifCaptureKey = KeyInterop.VirtualKeyFromKey(key);
            e.Handled = true;
        }

        public void PreviewKeyUp_D3DCapture(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
                return;

            D3dKey = KeyInterop.VirtualKeyFromKey(key);
            e.Handled = true;
        }

        #endregion

        #region Properties

        public bool FullscreenCtrl
        {
            get { return fullscreenCtrl; }
            set { SetProperty(ref fullscreenCtrl, value); }
        }

        public bool FullscreenShift
        {
            get { return fullscreenShift; }
            set { SetProperty(ref fullscreenShift, value); }
        }

        public bool FullscreenAlt
        {
            get { return fullscreenAlt; }
            set { SetProperty(ref fullscreenAlt, value); }
        }

        public bool CurrentwindowCtrl
        {
            get { return currentwindowCtrl; }
            set { SetProperty(ref currentwindowCtrl, value); }
        }

        public bool CurrentwindowShift
        {
            get { return currentwindowShift; }
            set { SetProperty(ref currentwindowShift, value); }
        }

        public bool CurrentwindowAlt
        {
            get { return currentwindowAlt; }
            set { SetProperty(ref currentwindowAlt, value); }
        }

        public bool SelectedareaCtrl
        {
            get { return selectedareaCtrl; }
            set { SetProperty(ref selectedareaCtrl, value); }
        }

        public bool SelectedareaShift
        {
            get { return selectedareaShift; }
            set { SetProperty(ref selectedareaShift, value); }
        }

        public bool SelectedareaAlt
        {
            get { return selectedareaAlt; }
            set { SetProperty(ref selectedareaAlt, value); }
        }

        public bool GifCaptureCtrl
        {
            get { return gifcaptureCtrl; }
            set { SetProperty(ref gifcaptureCtrl, value); }
        }

        public bool GifCaptureShift
        {
            get { return gifcaptureShift; }
            set { SetProperty(ref gifcaptureShift, value); }
        }

        public bool GifCaptureAlt
        {
            get { return gifcaptureAlt; }
            set { SetProperty(ref gifcaptureAlt, value); }
        }

        public bool D3dCtrl
        {
            get { return d3dCtrl; }
            set { SetProperty(ref d3dCtrl, value); }
        }

        public bool D3dShift
        {
            get { return d3dShift; }
            set { SetProperty(ref d3dShift, value); }
        }

        public bool D3dAlt
        {
            get { return d3dAlt; }
            set { SetProperty(ref d3dAlt, value); }
        }

        public int FullscreenKey
        {
            get { return fullscreenKey; }
            set { fullscreenKey = value; FullscreenString = KeyInterop.KeyFromVirtualKey(fullscreenKey).ToString(); }
        }

        public int CurrentwindowKey
        {
            get { return currentwindowKey; }
            set { currentwindowKey = value; CurrentwindowString = KeyInterop.KeyFromVirtualKey(currentwindowKey).ToString(); }
        }

        public int SelectedareaKey
        {
            get { return selectedareaKey; }
            set { selectedareaKey = value; SelectedareaString = KeyInterop.KeyFromVirtualKey(selectedareaKey).ToString(); }
        }

        public int GifCaptureKey
        {
            get { return gifcaptureKey; }
            set { gifcaptureKey = value; GifCaptureString = KeyInterop.KeyFromVirtualKey(gifcaptureKey).ToString(); }
        }

        public int D3dKey
        {
            get { return d3dKey; }
            set { d3dKey = value; D3dString = KeyInterop.KeyFromVirtualKey(d3dKey).ToString(); }
        }

        public string FullscreenString
        {
            get { return fullscreenString; }
            set { SetProperty(ref fullscreenString, value); }
        }

        public string CurrentwindowString
        {
            get { return currentwindowString; }
            set { SetProperty(ref currentwindowString, value); }
        }

        public string SelectedareaString
        {
            get { return selectedareaString; }
            set { SetProperty(ref selectedareaString, value); }
        }

        public string GifCaptureString
        {
            get { return gifcaptureString; }
            set { SetProperty(ref gifcaptureString, value); }
        }

        public string D3dString
        {
            get { return d3dString; }
            set { SetProperty(ref d3dString, value); }
        }

        public string TextFilepath
        {
            get { return textFilepath; }
            set { SetProperty(ref textFilepath, value); }
        }

        public string DateTimeString
        {
            get { return dateTimeString; }
            set { SetProperty(ref dateTimeString, value); }
        }

        public bool LocalEnabled
        {
            get { return localEnabled; }
            set
            {
                localEnabled = value;
                if (value == false && UploadEnabled == false)
                {
                    UploadEnabled = true;
                }
                OnPropertyChanged("LocalEnabled");
            }
        }

        public bool UploadEnabled
        {
            get { return uploadEnabled; }
            set
            {
                uploadEnabled = value;
                if (value == false && LocalEnabled == false)
                {
                    LocalEnabled = true;
                }
                OnPropertyChanged("UploadEnabled");
            }
        }

        public bool StartMinimized {
            get { return startMinimized; }
            set { SetProperty(ref startMinimized, value); }
        }
        public bool MinimizeToTray {
            get { return minimizeToTray; }
            set { SetProperty(ref minimizeToTray, value); }
        }
        public bool OpenInBrowser {
            get { return openInBrowser; }
            set { SetProperty(ref openInBrowser, value); }
        }
        public bool CloseToTray {
            get { return closetToTray; }
            set { SetProperty(ref closetToTray, value); }
        }
        public bool RunAtStart {
            get { return runAtStart; }
            set { SetProperty(ref runAtStart, value); }
        }
        public bool CopyToClipboard {
            get { return copyToClipboard; }
            set { SetProperty(ref copyToClipboard, value); }
        }
        public bool GifUpload {
            get { return gifUpload; }
            set { SetProperty(ref gifUpload, value); }
        }
        public bool GifEditor {
            get { return gifEditor; }
            set { SetProperty(ref gifEditor, value); }
        }
        public bool GifCaptureCursor {
            get { return gifCaptureCursor; }
            set { SetProperty(ref gifCaptureCursor, value); }
        }
        public int GifFramerate {
            get { return gifFramerate; }
            set { SetProperty(ref gifFramerate, value); }
        }
        public int GifDuration {
            get { return gifDuration; }
            set { SetProperty(ref gifDuration, value); }
        }
        public bool DetectExclusive {
            get { return detectExclusive; }
            set { SetProperty(ref detectExclusive, value); }
        }
        public bool FullscreenD3D {
            get { return fullscreenD3D; }
            set { SetProperty(ref fullscreenD3D, value); }
        }
        public string PuushApiKey {
            get { return puushApiKey; }
            set { SetProperty(ref puushApiKey, value); }
        }
        public string Username {
            get { return username; }
            set { SetProperty(ref username, value); }
        }
        public string LoginButtonText {
            get { return loginButtonText; }
            private set { SetProperty(ref loginButtonText, value); }
        }
        public string LoginButtonTextGyazo {
            get { return loginButtonTextGyazo; }
            private set { SetProperty(ref loginButtonTextGyazo, value); }
        }
        public string RegisterButtonText {
            get { return registerButtonText; }
            private set { SetProperty(ref registerButtonText, value); }
        }
        public bool RegisterEnabled {
            get { return registerEnabled; }
            private set { SetProperty(ref registerEnabled, value); }
        }

        public string StatusLabelText
        {
            get { return statusLabelText; }
            private set { SetProperty(ref statusLabelText, value); }
        }

        public int UploadValue
        {
            get { return uploadValue; }
            set
            {
                uploadValue = value;
                OnPropertyChanged("Value1");
                OnPropertyChanged("Value2");
                OnPropertyChanged("Value3");
            }
        }

        public bool AnonUpload
        {
            get { return anonUpload; }
            set
            {
                anonUpload = value;
                OnPropertyChanged("AnonOn");
                OnPropertyChanged("AnonOff");
            }
        }

        public bool Value1
        {
            get { return UploadValue.Equals(0); }
            set { UploadValue = 0; }
        }

        public bool Value2
        {
            get { return UploadValue.Equals(1); }
            set { UploadValue = 1; }
        }

        public bool Value3
        {
            get { return UploadValue.Equals(2); }
            set { UploadValue = 2; }
        }

        public bool AnonOn
        {
            get { return AnonUpload.Equals(true); }
            set { AnonUpload = true; }
        }

        public bool AnonOff
        {
            get { return AnonUpload.Equals(false); }
            set { AnonUpload = false; }
        }

        public Visibility AuthProgressVisibility
        {
            get { return authProgressVisibility; }
            private set { SetProperty(ref authProgressVisibility, value); }
        }

        public bool LoginEnabled
        {
            get { return loginEnabled; }
            private set { SetProperty(ref loginEnabled, value); }
        }
        public bool LoginEnabledGyazo
        {
            get { return loginEnabledGyazo; }
            private set { SetProperty(ref loginEnabledGyazo, value); }
        }

        #endregion

        private void SetValues()
        {
            TextFilepath = Properties.Settings.Default.filePath;
            DateTimeString = Properties.Settings.Default.dateTimeString;
            LocalEnabled = Properties.Settings.Default.saveLocal;
            UploadEnabled = Properties.Settings.Default.autoUpload;
            StartMinimized = Properties.Settings.Default.startMinimized;
            MinimizeToTray = Properties.Settings.Default.minimizeToTray;
            OpenInBrowser = Properties.Settings.Default.openInBrowser;
            CloseToTray = Properties.Settings.Default.closeToTray;
            RunAtStart = Properties.Settings.Default.runAtStart;
            CopyToClipboard = Properties.Settings.Default.lastToClipboard;

            GifUpload = Properties.Settings.Default.gifUpload;
            GifEditor = Properties.Settings.Default.gifEditorEnabled;
            GifCaptureCursor = Properties.Settings.Default.gifCaptureCursor;
            GifFramerate = Properties.Settings.Default.gifFrameRate;
            GifDuration = Properties.Settings.Default.gifDuration;

            DetectExclusive = Properties.Settings.Default.d3dAutoDetect;
            FullscreenD3D = Properties.Settings.Default.d3dAllScreens;

            if(Properties.Settings.Default.anonUpload)
            {

            }

            AnonUpload = Properties.Settings.Default.anonUpload;
            UploadValue = Properties.Settings.Default.upload_site;

            PuushApiKey = Properties.Settings.Default.puush_key;
            SetHotkeys();
            if (Properties.Settings.Default.username != "")
            {
                Username = Properties.Settings.Default.username;
                LoginButtonText = "Logout";
            }
            else
            {
                LoginButtonText = "Login";
                Username = "(Not logged in)";
            }
            if (Properties.Settings.Default.gyazoToken != "")
            {
                LoginButtonTextGyazo = "Logout";
            }
            else
            {
                LoginButtonTextGyazo = "Login";
            }

            if (Properties.Settings.Default.shellExtActive)
            {
                RegisterButtonText = "Unregister";
            }
            else
            {
                RegisterButtonText = "Register";
            }
            RegisterEnabled = true;
        }

        private void SetHotkeys()
        {
            if (Properties.Settings.Default.hkFullscreen != null)
            {
                FullscreenCtrl = Properties.Settings.Default.hkFullscreen.ctrl;
                FullscreenShift = Properties.Settings.Default.hkFullscreen.shift;
                FullscreenAlt = Properties.Settings.Default.hkFullscreen.alt;
                FullscreenKey = Properties.Settings.Default.hkFullscreen.vkKey;
            }
            else
            {
                Properties.Settings.Default.hkFullscreen = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F2));
                FullscreenCtrl = Properties.Settings.Default.hkFullscreen.ctrl;
                FullscreenShift = Properties.Settings.Default.hkFullscreen.shift;
                FullscreenAlt = Properties.Settings.Default.hkFullscreen.alt;
                FullscreenKey = Properties.Settings.Default.hkFullscreen.vkKey;
            }

            if (Properties.Settings.Default.hkCurrentwindow != null)
            {
                CurrentwindowCtrl = Properties.Settings.Default.hkCurrentwindow.ctrl;
                CurrentwindowShift = Properties.Settings.Default.hkCurrentwindow.shift;
                CurrentwindowAlt = Properties.Settings.Default.hkCurrentwindow.alt;
                CurrentwindowKey = Properties.Settings.Default.hkCurrentwindow.vkKey;
            }
            else
            {
                Properties.Settings.Default.hkCurrentwindow = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F3));
                CurrentwindowCtrl = Properties.Settings.Default.hkCurrentwindow.ctrl;
                CurrentwindowShift = Properties.Settings.Default.hkCurrentwindow.shift;
                CurrentwindowAlt = Properties.Settings.Default.hkCurrentwindow.alt;
                CurrentwindowKey = Properties.Settings.Default.hkCurrentwindow.vkKey;
            }

            if (Properties.Settings.Default.hkSelectedarea != null)
            {
                SelectedareaCtrl = Properties.Settings.Default.hkSelectedarea.ctrl;
                SelectedareaShift = Properties.Settings.Default.hkSelectedarea.shift;
                SelectedareaAlt = Properties.Settings.Default.hkSelectedarea.alt;
                SelectedareaKey = Properties.Settings.Default.hkSelectedarea.vkKey;
            }
            else
            {
                Properties.Settings.Default.hkSelectedarea = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F4));
                SelectedareaCtrl = Properties.Settings.Default.hkSelectedarea.ctrl;
                SelectedareaShift = Properties.Settings.Default.hkSelectedarea.shift;
                SelectedareaAlt = Properties.Settings.Default.hkSelectedarea.alt;
                SelectedareaKey = Properties.Settings.Default.hkSelectedarea.vkKey;
            }

            if (Properties.Settings.Default.hkGifcapture != null)
            {
                GifCaptureCtrl = Properties.Settings.Default.hkGifcapture.ctrl;
                GifCaptureShift = Properties.Settings.Default.hkGifcapture.shift;
                GifCaptureAlt = Properties.Settings.Default.hkGifcapture.alt;
                GifCaptureKey = Properties.Settings.Default.hkGifcapture.vkKey;
            }
            else
            {
                Properties.Settings.Default.hkGifcapture = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F5));
                GifCaptureCtrl = Properties.Settings.Default.hkGifcapture.ctrl;
                GifCaptureShift = Properties.Settings.Default.hkGifcapture.shift;
                GifCaptureAlt = Properties.Settings.Default.hkGifcapture.alt;
                GifCaptureKey = Properties.Settings.Default.hkGifcapture.vkKey;
            }

            if (Properties.Settings.Default.hkD3DCap != null)
            {
                D3dCtrl = Properties.Settings.Default.hkD3DCap.ctrl;
                D3dShift = Properties.Settings.Default.hkD3DCap.shift;
                D3dAlt = Properties.Settings.Default.hkD3DCap.alt;
                D3dKey = Properties.Settings.Default.hkD3DCap.vkKey;
            }
            else
            {
                Properties.Settings.Default.hkD3DCap = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F6));
                D3dCtrl = Properties.Settings.Default.hkD3DCap.ctrl;
                D3dShift = Properties.Settings.Default.hkD3DCap.shift;
                D3dAlt = Properties.Settings.Default.hkD3DCap.alt;
                D3dKey = Properties.Settings.Default.hkD3DCap.vkKey;
            }
        }

        private void Cancel()
        {
            if (this.notification != null)
            {
                this.notification.Confirmed = false;
            }
            this.FinishInteraction();
        }

        private void Confirm()
        {
            if (UploadValue == 2 && PuushApiKey.Length < 1)
            {
                StatusLabelText = "Enter Puush API Key";
                return;
            }
            if (fullscreenKey != 0)
            {
                Properties.Settings.Default.hkFullscreen = new HotKey(fullscreenCtrl, fullscreenAlt, fullscreenShift, fullscreenKey);
            }
            if (currentwindowKey != 0)
            {
                Properties.Settings.Default.hkCurrentwindow = new HotKey(currentwindowCtrl, currentwindowAlt, currentwindowShift, currentwindowKey);
            }
            if (selectedareaKey != 0)
            {
                Properties.Settings.Default.hkSelectedarea = new HotKey(selectedareaCtrl, selectedareaAlt, selectedareaShift, selectedareaKey);
            }
            if (gifcaptureKey != 0)
            {
                Properties.Settings.Default.hkGifcapture = new HotKey(gifcaptureCtrl, gifcaptureAlt, gifcaptureShift, gifcaptureKey);
            }
            if (d3dKey != 0)
            {
                Properties.Settings.Default.hkD3DCap = new HotKey(d3dCtrl, d3dAlt, d3dShift, d3dKey);
            }

            Properties.Settings.Default.upload_site = UploadValue;

            SetStartUp(RunAtStart);

            Properties.Settings.Default.lastToClipboard = CopyToClipboard;
            Properties.Settings.Default.minimizeToTray = MinimizeToTray;
            Properties.Settings.Default.openInBrowser = OpenInBrowser;
            Properties.Settings.Default.runAtStart = RunAtStart;
            Properties.Settings.Default.saveLocal = LocalEnabled;
            Properties.Settings.Default.anonUpload = AnonUpload;
            Properties.Settings.Default.autoUpload = UploadEnabled;
            Properties.Settings.Default.closeToTray = CloseToTray;
            Properties.Settings.Default.startMinimized = StartMinimized;

            Properties.Settings.Default.gifUpload = GifUpload;
            Properties.Settings.Default.gifEditorEnabled = GifEditor;
            Properties.Settings.Default.gifCaptureCursor = GifCaptureCursor;
            Properties.Settings.Default.gifFrameRate = GifFramerate;
            Properties.Settings.Default.gifDuration = GifDuration;

            Properties.Settings.Default.d3dAllScreens = FullscreenD3D;
            Properties.Settings.Default.d3dAutoDetect = DetectExclusive;

            Properties.Settings.Default.puush_key = PuushApiKey;
            if (Directory.Exists(TextFilepath))
            {
                Properties.Settings.Default.filePath = TextFilepath;
            }
            else
            {
                Properties.Settings.Default.filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            Properties.Settings.Default.dateTimeString = DateTimeString;

            if (this.notification != null)
            {
                this.notification.Confirmed = true;
            }
            this.FinishInteraction();
        }

        private static void SetStartUp(bool s)
        {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (s)
                {
                    if (rk.GetValue("LXtory") == null)
                    {
                        rk.SetValue("LXtory", Assembly.GetExecutingAssembly().Location);
                    }
                }
                else
                {
                    if (rk.GetValue("LXtory") != null)
                    {
                        rk.DeleteValue("LXtory");
                    }
                }
            }
        }

        private async void GyazoLogin()
        {
            if (Properties.Settings.Default.gyazoToken == string.Empty)
            {
                statusLabelText = StatusLabelText = "Waiting for Authorization..";
                AuthProgressVisibility = Visibility.Visible;
                LoginEnabledGyazo = false;
                try
                {
                    string authCode = await GetAuthCode("https://api.gyazo.com/oauth/authorize?response_type=code&client_id=f6f7ea4ac48869d64d585050fb041a9a85b28f531a1a43833028f75a0a3a6183&redirect_uri=http%3A%2F%2Flocalhost%3A8080%2FLXtory_Auth%2F&scope=public");
                    if (authCode != string.Empty)
                    {
                        // get access token
                        await Uploader.GetGyazoToken(authCode);
                        StatusLabelText = "Authorization complete";
                        LoginButtonTextGyazo = "Logout";
                        LoginEnabledGyazo = true;
                        AuthProgressVisibility = Visibility.Hidden;
                    }
                    else
                    {
                        // auth failed
                        AuthProgressVisibility = Visibility.Hidden;
                        LoginEnabledGyazo = true;
                        StatusLabelText = "Authorization failed";
                    }
                }
                catch (Exception)
                {
                    AuthProgressVisibility = Visibility.Hidden;
                    LoginEnabledGyazo = true;
                    StatusLabelText = "Authorization failed";
                }
            }
            else
            {
                Properties.Settings.Default.gyazoToken = "";
                Properties.Settings.Default.Save();
                LoginButtonTextGyazo = "Login";
            }
        }

        private async void Login()
        {
            if (Properties.Settings.Default.username == string.Empty && Properties.Settings.Default.accessToken == string.Empty)
            {
                StatusLabelText = "Waiting for Authorization..";
                AuthProgressVisibility = Visibility.Visible;
                LoginEnabled = false;
                try
                {
                    string authCode = await GetAuthCode("https://api.imgur.com/oauth2/authorize?client_id=83c1c8bf9f4d2b1&response_type=code&state=LXtory");
                    if (authCode != string.Empty)
                    {
                        //get tokens
                        await Uploader.GetToken(authCode);
                        Username = Properties.Settings.Default.username;
                        StatusLabelText = "Authorization complete";
                        LoginButtonText = "Logout";
                        LoginEnabled = true;
                        AuthProgressVisibility = Visibility.Hidden;
                    }
                    else
                    {
                        // auth failed
                        AuthProgressVisibility = Visibility.Hidden;
                        LoginEnabled = true;
                        StatusLabelText = "Authorization failed";
                    }
                }
                catch (Exception)
                {
                    AuthProgressVisibility = Visibility.Hidden;
                    LoginEnabled = true;
                    StatusLabelText = "Authorization failed";
                }
            }
            else
            {
                Properties.Settings.Default.accessToken = "";
                Properties.Settings.Default.refreshToken = "";
                Properties.Settings.Default.username = "";
                Properties.Settings.Default.Save();
                LoginButtonText = "Login";
                Username = "Not logged in";
            }
        }

        private async Task<string> GetAuthCode(string url)
        {
            //IPAddress local = IPAddress.Loopback;
            TcpListener listener = new TcpListener(IPAddress.Loopback, 8080);
            listener.Start();
            Byte[] bytes = new Byte[256];
            bool receiving = true;
            do
            {
                Console.WriteLine(@"WAITING CONNECTION");
                //Process.Start("https://api.imgur.com/oauth2/authorize?client_id=83c1c8bf9f4d2b1&response_type=code&state=LXtory");
                Process.Start(url);
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine(@"CONNECTED");
                NetworkStream stream = client.GetStream();
                int i;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0 && receiving)
                {
                    var data = Encoding.ASCII.GetString(bytes, 0, i);
                    if (data.StartsWith("GET /LXtory_Auth/"))
                    {
                        receiving = false;
                        var regex = new Regex(@"code=(.*?) ");
                        var result = regex.Match(data);
                        if (result.Success)
                        {
                            accesscode = result.Groups[1].Value;
                            byte[] msg = Encoding.ASCII.GetBytes(CloseWindowResponse);
                            stream.Write(msg, 0, msg.Length);
                        }
                    }
                    //Console.WriteLine("Received: {0}", data);
                    //data = data.ToUpper();

                    /*byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: {0}", data);*/
                }
                client.Close();
            } while (receiving);

            /*System.Net.HttpListener listener = new System.Net.HttpListener();
                listener.Prefixes.Add("http://localhost:80/");
                listener.Start();
                Process.Start("https://api.imgur.com/oauth2/authorize?client_id=83c1c8bf9f4d2b1&response_type=code&state=LXtory");
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;

                if (request.Url.AbsolutePath == "/LXtory_Auth/")
                {
                    accesscode = request.Url.Query.Substring(request.Url.Query.LastIndexOf('=') + 1);
                }
                HttpListenerResponse response = context.Response;
                byte[] buffer = Encoding.UTF8.GetBytes(CloseWindowResponse);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                listener.Stop();*/
            listener.Stop();
            return accesscode;
        }

        private static void AddContextMenuItems()
        {
            string executablePath = Assembly.GetExecutingAssembly().Location;
            List<string> keys = new List<string> {
                "SOFTWARE\\Classes\\giffile\\shell\\LXtory",
                "SOFTWARE\\Classes\\jpegfile\\shell\\LXtory",
                "SOFTWARE\\Classes\\pngfile\\shell\\LXtory",
                "SOFTWARE\\Classes\\SystemFileAssociations\\image\\shell\\LXtory" };

            foreach (string k in keys)
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(k))
                {
                    if (key != null)
                    {
                        key.SetValue("", "Upload With LXtory");
                        key.SetValue("Icon", $"\"{executablePath}\"");
                        RegistryKey subkey = key.CreateSubKey("command");
                        if (subkey != null)
                        {
                            subkey.SetValue("", $"\"{executablePath}\" \"%1\"");
                            subkey.Close();
                        }
                    }
                }
            }
        }

        private static void RemoveContextMenuItems()
        {
            List<string> keys = new List<string> {
                "SOFTWARE\\Classes\\giffile\\shell\\LXtory",
                "SOFTWARE\\Classes\\jpegfile\\shell\\LXtory",
                "SOFTWARE\\Classes\\pngfile\\shell\\LXtory",
                "SOFTWARE\\Classes\\SystemFileAssociations\\image\\shell\\LXtory" };

            foreach (string key in keys)
            {
                Registry.CurrentUser.DeleteSubKeyTree(key, false);
            }
        }

        private void Register()
        {
            if (Properties.Settings.Default.shellExtActive)
            {
                RemoveContextMenuItems();
                Properties.Settings.Default.shellExtActive = false;
                RegisterButtonText = "Register";
            }
            else
            {
                AddContextMenuItems();
                Properties.Settings.Default.shellExtActive = true;
                RegisterButtonText = "Unregister";
            }
        }
    }
}
