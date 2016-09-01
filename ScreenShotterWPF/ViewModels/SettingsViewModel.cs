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
using System.Windows.Controls;
using Prism.Commands;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ScreenShotterWPF.ViewModels
{
    public class SettingsViewModel : BindableBase, IInteractionRequestAware
    {
        private SettingsNotification notification;
        private static readonly Properties.Settings settings = Properties.Settings.Default;

        public Action FinishInteraction { get; set; }
        
        private string accesscode = string.Empty;

        private const string CloseWindowResponse = "<!DOCTYPE html><html><head></head><body style=\"background-color: #121211; font-family: Arial,sans-serif;    color: rgb(221, 221, 209);\"><h1>Authorization Successfull</h1><p>You can now close this window</p></body></html>";

        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand BrowseCommand { get; private set; }
        public ICommand LoginCommand { get; private set; }
        public ICommand LoginCommandGyazo { get; private set; }
        public ICommand RegisterCommand { get; private set; }

        public ICommand BrowseKeyCommand { get; private set; }
        public ICommand PasswordChangedCommand { get; private set; }
        public ICommand PassphraseChangedCommand { get; private set; }

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
        private bool disableWebThumbs;

        private string ftpHost;
        private int ftpPort;
        private string ftpPath;
        private string ftpUsername;
        private string ftpKeyfile;
        private string ftpPassword;
        private string ftpPassphrase;
        private int ftpMethod;
        private int ftpProtocol;

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
            this.BrowseKeyCommand = new DelegateCommand(BrowseKey);
            this.PasswordChangedCommand = new DelegateCommand<PasswordBox>(PasswordChanged);
            this.PassphraseChangedCommand = new DelegateCommand<PasswordBox>(PassphraseChanged);
            AuthProgressVisibility = Visibility.Hidden;
        }

        private void PasswordChanged(PasswordBox obj)
        {
            FTPPassword = obj.Password;
        }

        private void PassphraseChanged(PasswordBox obj)
        {
            FTPPassphrase = obj.Password;
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

        private void BrowseKey()
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Key File...",
                IsFolderPicker = false,
                InitialDirectory = this.ftpKeyfile,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var key = dialog.FileName;
                FTPKeyfile = key;
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

        public bool DisableWebThumbs
        {
            get { return disableWebThumbs; }
            set { SetProperty(ref disableWebThumbs, value); }
        }

        public Dictionary<int, string> FTPMethods
        {
            get
            {
                return new Dictionary<int, string>
                {
                    [0] = "Password",
                    [1] = "Publickey"
                };
            }
        }

        public Dictionary<int, string> FTPProtocols
        {
            get
            {
                return new Dictionary<int, string>
                {
                    [0] = "ftp://",
                    [1] = "sftp://"
                };
            }
        }

        public int FTPMethod
        {
            get { return ftpMethod; }
            set { SetProperty(ref ftpMethod, value); }
        }

        public int FTPProtocol
        {
            get { return ftpProtocol; }
            set { SetProperty(ref ftpProtocol, value); }
        }

        public string FTPPassphrase
        {
            get { return ftpPassphrase; }
            private set { SetProperty(ref ftpPassphrase, value); }
        }

        public string FTPPassword
        {
            get { return ftpPassword; }
            private set { SetProperty(ref ftpPassword, value); }
        }

        public string FTPUsername
        {
            get { return ftpUsername; }
            set { SetProperty(ref ftpUsername, value); }
        }

        public string FTPPath
        {
            get { return ftpPath; }
            set { SetProperty(ref ftpPath, value); }
        }

        public string FTPPort
        {
            get { return ftpPort.ToString(); }
            set
            {
                int port;
                if (!Int32.TryParse(value, out port))
                {
                    SetProperty(ref ftpPort, 22);
                }
                else
                {
                    SetProperty(ref ftpPort, port);
                }
            }
        }

        public string FTPHost
        {
            get { return ftpHost; }
            set { SetProperty(ref ftpHost, value); }
        }

        public string FTPKeyfile
        {
            get { return ftpKeyfile; }
            set { SetProperty(ref ftpKeyfile, value); }
        }

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
                OnPropertyChanged("Value4");
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

        public bool Value4
        {
            get { return UploadValue.Equals(3); }
            set { UploadValue = 3; }
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
            TextFilepath = settings.filePath;
            DateTimeString = settings.dateTimeString;
            LocalEnabled = settings.saveLocal;
            UploadEnabled = settings.autoUpload;
            StartMinimized = settings.startMinimized;
            MinimizeToTray = settings.minimizeToTray;
            OpenInBrowser = settings.openInBrowser;
            CloseToTray = settings.closeToTray;
            RunAtStart = settings.runAtStart;
            CopyToClipboard = settings.lastToClipboard;

            GifUpload = settings.gifUpload;
            GifEditor = settings.gifEditorEnabled;
            GifCaptureCursor = settings.gifCaptureCursor;
            GifFramerate = settings.gifFrameRate;
            GifDuration = settings.gifDuration;

            DetectExclusive = settings.d3dAutoDetect;
            FullscreenD3D = settings.d3dAllScreens;

            DisableWebThumbs = settings.disableWebThumbs;

            FTPHost = settings.ftpHost;
            FTPPort = settings.ftpPort.ToString();
            FTPPath = settings.ftpPath;
            FTPUsername = settings.ftpUsername;
            FTPPassword = settings.ftpPassword;
            FTPKeyfile = settings.ftpKeyfile;
            FTPPassphrase = settings.ftpPassphrase;
            FTPMethod = settings.ftpMethod;
            FTPProtocol = settings.ftpProtocol;

            if(settings.anonUpload)
            {

            }

            AnonUpload = settings.anonUpload;
            UploadValue = settings.upload_site;

            PuushApiKey = settings.puush_key;
            SetHotkeys();
            if (settings.username != "")
            {
                Username = settings.username;
                LoginButtonText = "Logout";
            }
            else
            {
                LoginButtonText = "Login";
                Username = "(Not logged in)";
            }
            if (settings.gyazoToken != "")
            {
                LoginButtonTextGyazo = "Logout";
            }
            else
            {
                LoginButtonTextGyazo = "Login";
            }

            if (settings.shellExtActive)
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
            if (settings.hkFullscreen != null)
            {
                FullscreenCtrl = settings.hkFullscreen.ctrl;
                FullscreenShift = settings.hkFullscreen.shift;
                FullscreenAlt = settings.hkFullscreen.alt;
                FullscreenKey = settings.hkFullscreen.vkKey;
            }
            else
            {
                settings.hkFullscreen = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F2));
                FullscreenCtrl = settings.hkFullscreen.ctrl;
                FullscreenShift = settings.hkFullscreen.shift;
                FullscreenAlt = settings.hkFullscreen.alt;
                FullscreenKey = settings.hkFullscreen.vkKey;
            }

            if (settings.hkCurrentwindow != null)
            {
                CurrentwindowCtrl = settings.hkCurrentwindow.ctrl;
                CurrentwindowShift = settings.hkCurrentwindow.shift;
                CurrentwindowAlt = settings.hkCurrentwindow.alt;
                CurrentwindowKey = settings.hkCurrentwindow.vkKey;
            }
            else
            {
                settings.hkCurrentwindow = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F3));
                CurrentwindowCtrl = settings.hkCurrentwindow.ctrl;
                CurrentwindowShift = settings.hkCurrentwindow.shift;
                CurrentwindowAlt = settings.hkCurrentwindow.alt;
                CurrentwindowKey = settings.hkCurrentwindow.vkKey;
            }

            if (settings.hkSelectedarea != null)
            {
                SelectedareaCtrl = settings.hkSelectedarea.ctrl;
                SelectedareaShift = settings.hkSelectedarea.shift;
                SelectedareaAlt = settings.hkSelectedarea.alt;
                SelectedareaKey = settings.hkSelectedarea.vkKey;
            }
            else
            {
                settings.hkSelectedarea = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F4));
                SelectedareaCtrl = settings.hkSelectedarea.ctrl;
                SelectedareaShift = settings.hkSelectedarea.shift;
                SelectedareaAlt = settings.hkSelectedarea.alt;
                SelectedareaKey = settings.hkSelectedarea.vkKey;
            }

            if (settings.hkGifcapture != null)
            {
                GifCaptureCtrl = settings.hkGifcapture.ctrl;
                GifCaptureShift = settings.hkGifcapture.shift;
                GifCaptureAlt = settings.hkGifcapture.alt;
                GifCaptureKey = settings.hkGifcapture.vkKey;
            }
            else
            {
                settings.hkGifcapture = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F5));
                GifCaptureCtrl = settings.hkGifcapture.ctrl;
                GifCaptureShift = settings.hkGifcapture.shift;
                GifCaptureAlt = settings.hkGifcapture.alt;
                GifCaptureKey = settings.hkGifcapture.vkKey;
            }

            if (settings.hkD3DCap != null)
            {
                D3dCtrl = settings.hkD3DCap.ctrl;
                D3dShift = settings.hkD3DCap.shift;
                D3dAlt = settings.hkD3DCap.alt;
                D3dKey = settings.hkD3DCap.vkKey;
            }
            else
            {
                settings.hkD3DCap = new HotKey(true, false, false, KeyInterop.VirtualKeyFromKey(Key.F6));
                D3dCtrl = settings.hkD3DCap.ctrl;
                D3dShift = settings.hkD3DCap.shift;
                D3dAlt = settings.hkD3DCap.alt;
                D3dKey = settings.hkD3DCap.vkKey;
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
                settings.hkFullscreen = new HotKey(fullscreenCtrl, fullscreenAlt, fullscreenShift, fullscreenKey);
            }
            if (currentwindowKey != 0)
            {
                settings.hkCurrentwindow = new HotKey(currentwindowCtrl, currentwindowAlt, currentwindowShift, currentwindowKey);
            }
            if (selectedareaKey != 0)
            {
                settings.hkSelectedarea = new HotKey(selectedareaCtrl, selectedareaAlt, selectedareaShift, selectedareaKey);
            }
            if (gifcaptureKey != 0)
            {
                settings.hkGifcapture = new HotKey(gifcaptureCtrl, gifcaptureAlt, gifcaptureShift, gifcaptureKey);
            }
            if (d3dKey != 0)
            {
                settings.hkD3DCap = new HotKey(d3dCtrl, d3dAlt, d3dShift, d3dKey);
            }

            settings.upload_site = UploadValue;

            SetStartUp(RunAtStart);

            settings.lastToClipboard = CopyToClipboard;
            settings.minimizeToTray = MinimizeToTray;
            settings.openInBrowser = OpenInBrowser;
            settings.runAtStart = RunAtStart;
            settings.saveLocal = LocalEnabled;
            settings.anonUpload = AnonUpload;
            settings.autoUpload = UploadEnabled;
            settings.closeToTray = CloseToTray;
            settings.startMinimized = StartMinimized;

            settings.gifUpload = GifUpload;
            settings.gifEditorEnabled = GifEditor;
            settings.gifCaptureCursor = GifCaptureCursor;
            settings.gifFrameRate = GifFramerate;
            settings.gifDuration = GifDuration;

            settings.d3dAllScreens = FullscreenD3D;
            settings.d3dAutoDetect = DetectExclusive;

            settings.puush_key = PuushApiKey;

            settings.disableWebThumbs = DisableWebThumbs;

            settings.ftpHost = FTPHost;
            settings.ftpPort = ftpPort;
            settings.ftpPath = FTPPath;
            settings.ftpUsername = FTPUsername;
            settings.ftpPassword = FTPPassword;
            settings.ftpKeyfile = FTPKeyfile;
            settings.ftpPassphrase = FTPPassphrase;
            settings.ftpMethod = FTPMethod;
            settings.ftpProtocol = FTPProtocol;

            if (Directory.Exists(TextFilepath))
            {
                settings.filePath = TextFilepath;
            }
            else
            {
                settings.filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            settings.dateTimeString = DateTimeString;

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
            if (settings.gyazoToken == string.Empty)
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
                settings.gyazoToken = "";
                settings.Save();
                LoginButtonTextGyazo = "Login";
            }
        }

        private async void Login()
        {
            if (settings.username == string.Empty && settings.accessToken == string.Empty)
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
                        Username = settings.username;
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
                settings.accessToken = "";
                settings.refreshToken = "";
                settings.username = "";
                settings.Save();
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
            if (settings.shellExtActive)
            {
                RemoveContextMenuItems();
                settings.shellExtActive = false;
                RegisterButtonText = "Register";
            }
            else
            {
                AddContextMenuItems();
                settings.shellExtActive = true;
                RegisterButtonText = "Unregister";
            }
        }
    }
}
