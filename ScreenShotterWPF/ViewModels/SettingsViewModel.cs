using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ScreenShotterWPF.Notifications;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System.IO;
using System.Net;
//using System.Net.Sockets;
using Microsoft.Win32;
using System.Reflection;
using System.Security.Cryptography;
//using System.Security.Principal;
//using System.Text.RegularExpressions;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Prism.Commands;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ScreenShotterWPF.ViewModels
{
    public class SettingsViewModel : BindableBase, IInteractionRequestAware
    {
        private SettingsNotification notification;
        private static readonly Properties.Settings settings = Properties.Settings.Default;

        public Action FinishInteraction { get; set; }

        private const string CloseWindowResponse = "<!DOCTYPE html><html><head></head><body style=\"background-color: #121211; font-family: Arial,sans-serif;    color: rgb(221, 221, 209);\"><h1>Authorization Successful</h1><p>You can now close this window</p></body></html>";

        public ICommand ConfirmCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand BrowseCommand { get; private set; }
        public ICommand LoginCommand { get; private set; }

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
        private string statusLabelText;
        //private bool anonUpload;
        private bool imgurUploadToAccount;
        private UploadSite uploadValue;
        private UploadSite fileuploadValue;
        private string dateTimeString;
        private bool disableWebThumbs;
        private string dropboxPath;

        private string ftpHost;
        private int ftpPort;
        private string ftpPath;
        private string ftpUsername;
        private string ftpKeyfile;
        private string ftpPassword;
        private string ftpPassphrase;
        private int ftpMethod;
        private int ftpProtocol;

        private static bool contextMenuEnabled;
        private static bool fileUploadEnabled;
        private string loginButtonTextDropbox;
        private string loginButtonTextGDrive;

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
                    //this.OnPropertyChanged(() => this.Notification);
                    this.RaisePropertyChanged(nameof(this.Notification));
                }
            }
        }

        public SettingsViewModel()
        {
            //settingsBrowse.IsEnabled = true;
            this.ConfirmCommand = new DelegateCommand(Confirm);
            this.CancelCommand = new DelegateCommand(Cancel);
            this.BrowseCommand = new DelegateCommand(Browse);
            this.LoginCommand = new DelegateCommand<UploadSite?>(Login);
            this.BrowseKeyCommand = new DelegateCommand(BrowseKey);
            this.PasswordChangedCommand = new DelegateCommand<PasswordBox>(PasswordChanged);
            this.PassphraseChangedCommand = new DelegateCommand<PasswordBox>(PassphraseChanged);
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

        public bool ImgurUploadToAccount
        {
            get { return imgurUploadToAccount; }
            set { SetProperty(ref imgurUploadToAccount, value); }
        }

        public bool FileUploadEnabled
        {
            get { return fileUploadEnabled; }
            set { SetProperty(ref fileUploadEnabled, value); }
        }

        public bool ContextMenuEnabled
        {
            get { return contextMenuEnabled; }
            set { SetProperty(ref contextMenuEnabled, value); }
        }

        public bool DisableWebThumbs
        {
            get { return disableWebThumbs; }
            set { SetProperty(ref disableWebThumbs, value); }
        }

        public string DropboxPath
        {
            get { return dropboxPath; }
            set { SetProperty(ref dropboxPath, value); }
        }

        public static Dictionary<string, UploadSite> ImageUploadSites
        {
            get
            {
                return new Dictionary<string, UploadSite>
                {
                    ["Imgur"] = UploadSite.Imgur,
                    ["Gyazo"] = UploadSite.Gyazo,
                    ["Puush"] = UploadSite.Puush,
                    ["Dropbox"] = UploadSite.Dropbox,
                    ["Google Drive"] = UploadSite.GoogleDrive,
                    ["S/FTP"] = UploadSite.SFTP
                };
            }
        }

        public static Dictionary<string, UploadSite> FileUploadSites
        {
            get
            {
                return new Dictionary<string, UploadSite>
                {
                    ["None"] = UploadSite.None,
                    ["Puush"] = UploadSite.Puush,
                    ["Dropbox"] = UploadSite.Dropbox,
                    ["Google Drive"] = UploadSite.GoogleDrive,
                    ["S/FTP"] = UploadSite.SFTP
                };
            }
        }

        public static Dictionary<int, string> FTPMethods
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

        public static Dictionary<int, string> FTPProtocols
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
                if (!Int32.TryParse(value, out int port))
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
                //OnPropertyChanged("LocalEnabled");
                RaisePropertyChanged("LocalEnabled");
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
                //OnPropertyChanged("UploadEnabled");
                RaisePropertyChanged("UploadEnabled");
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
        public string LoginButtonTextDropbox {
            get { return loginButtonTextDropbox; }
            private set { SetProperty(ref loginButtonTextDropbox, value); }
        }
        public string LoginButtonTextGDrive {
            get { return loginButtonTextGDrive; }
            private set { SetProperty(ref loginButtonTextGDrive, value); }
        }

        public string StatusLabelText
        {
            get { return statusLabelText; }
            private set { SetProperty(ref statusLabelText, value); }
        }

        public UploadSite UploadValue
        {
            get { return uploadValue; }
            set { SetProperty(ref uploadValue, value); }
        }

        public UploadSite FileuploadValue
        {
            get { return fileuploadValue; }
            set { SetProperty(ref fileuploadValue, value); }
        }

        public string FileSizeImgur
        {
            get { return settings.fileSizeImgur.ToString(); }
        }

        public string FileSizePuush
        {
            get { return settings.fileSizePuush.ToString(); }
        }

        public string FileSizeGyazo
        {
            get { return settings.fileSizeGyazo.ToString(); }
        }

        public string FileSizeGDrive
        {
            get { return settings.fileSizeGDrive.ToString(); }
        }

        public string FileSizeDropbox
        {
            get { return settings.fileSizeDropbox.ToString(); }
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

            ContextMenuEnabled = settings.shellExtActive;
            FileUploadEnabled = settings.fileUploadEnabled;

            ImgurUploadToAccount = !settings.anonUpload;
            UploadValue = settings.imageUploadSite;
            FileuploadValue = settings.fileUploadSite;
            DropboxPath = settings.dropboxPath;
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
            LoginButtonTextGyazo = settings.gyazoToken != "" ? "Logout" : "Login";
            LoginButtonTextDropbox = settings.dropboxToken != "" ? "Logout" : "Login";
            LoginButtonTextGDrive = settings.gdriveToken != "" ? "Logout" : "Login";
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
            if (UploadValue == UploadSite.Puush || FileuploadValue == UploadSite.Puush)
            {
                if (PuushApiKey.Length < 1)
                {
                    StatusLabelText = "Enter Puush API Key";
                    BalloonMessage.ShowMessage("Enter Puush API Key", BalloonIcon.Warning);
                    return;
                }
            }
            if (UploadValue == UploadSite.Gyazo && settings.gyazoToken == string.Empty)
            {
                StatusLabelText = "Gyazo login needed";
                BalloonMessage.ShowMessage("Gyazo login needed", BalloonIcon.Warning);
                return;
            }
            if (UploadValue == UploadSite.Imgur && imgurUploadToAccount && settings.accessToken == string.Empty)
            {
                StatusLabelText = "Imgur login needed";
                BalloonMessage.ShowMessage("Imgur login needed", BalloonIcon.Warning);
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

            settings.imageUploadSite = UploadValue;
            settings.fileUploadSite = FileuploadValue;

            SetStartUp(RunAtStart);

            settings.lastToClipboard = CopyToClipboard;
            settings.minimizeToTray = MinimizeToTray;
            settings.openInBrowser = OpenInBrowser;
            settings.runAtStart = RunAtStart;
            settings.saveLocal = LocalEnabled;
            settings.anonUpload = !ImgurUploadToAccount;
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
            settings.dropboxPath = DropboxPath;
            settings.filePath = Directory.Exists(TextFilepath) ? TextFilepath : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            if (ContextMenuEnabled != settings.shellExtActive || FileUploadEnabled != settings.fileUploadEnabled)
            {
                if (!ContextMenuEnabled)
                {
                    RemoveContextMenu();
                }
                else
                {
                    EnableContextMenu(!FileUploadEnabled);
                }
            }
            settings.shellExtActive = ContextMenuEnabled;
            settings.fileUploadEnabled = FileUploadEnabled;

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

        private void Login(UploadSite? site)
        {
            switch (site)
            {
                case UploadSite.Imgur:
                    ImgurLogin();
                    break;
                case UploadSite.Gyazo:
                    GyazoLogin();
                    break;
                case UploadSite.Puush:
                    break;
                case UploadSite.SFTP:
                    break;
                case UploadSite.Dropbox:
                    DropboxLogin();
                    break;
                case UploadSite.GoogleDrive:
                    GoogleDriveLogin();
                    break;
                case UploadSite.None:
                    break;
                default:
                    break;
            }
        }

        private async void GoogleDriveLogin()
        {
            if (settings.gdriveToken == string.Empty)
            {
                StatusLabelText = "Waiting for Authorization..";
                try
                {
                    string code_verifier = randomDataBase64url(32);
                    string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));
                    const string code_challenge_method = "S256";

                    string endpoint = "https://accounts.google.com/o/oauth2/v2/auth";
                    string scope = "openid%20profile%20https://www.googleapis.com/auth/drive";
                    string redirect_uri = "http://127.0.0.1:12745/";

                    var code = await GetAuthCode2($"{endpoint}?response_type=code&scope={scope}&redirect_uri={redirect_uri}&client_id={OAuthHelpers.GoogleDriveID}&code_challenge={code_challenge}&code_challenge_method={code_challenge_method}", redirect_uri);
                    if (code.Get("code") != null)
                    {
                        await OAuthHelpers.GetGoogleDriveToken(code.Get("code"), redirect_uri, code_verifier);
                        StatusLabelText = "Authorization complete";
                        LoginButtonTextGDrive = "Logout";
                        BalloonMessage.ShowMessage("Authorization complete", BalloonIcon.Info);
                    }
                }
                catch (Exception)
                {
                    StatusLabelText = "Authorization failed";
                    BalloonMessage.ShowMessage("Authorization failed", BalloonIcon.Error);
                }
            }
            else
            {
                settings.gdriveToken = "";
                LoginButtonTextGDrive = "Login";
                settings.Save();
            }
        }

        private async Task<NameValueCollection> GetAuthCode2(string uri, string redirectUri)
        {
            var http = new HttpListener();
            http.Prefixes.Add(redirectUri);
            Console.WriteLine("Listening..");
            http.Start();
            
            Process.Start(uri);

            var context = await http.GetContextAsync();
            var response = context.Response;
            var buffer = Encoding.UTF8.GetBytes(CloseWindowResponse);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            Task t = output.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            {
                output.Close();
                http.Stop();
                Console.WriteLine("Listener stopped");
            });

            return context.Request.QueryString;
        }

        private async void DropboxLogin()
        {
            if (settings.dropboxToken == string.Empty)
            {
                StatusLabelText = "Waiting for Authorization..";
                try
                {
                    //string authCode = await GetAuthCode("https://www.dropbox.com/oauth2/authorize?response_type=code&client_id=r36i3mn05mghy8d&redirect_uri=http%3A%2F%2Flocalhost%3A8080%2FLXtory_Auth%2F");
                    var authCode = await GetAuthCode2("https://www.dropbox.com/oauth2/authorize?response_type=code&client_id=r36i3mn05mghy8d&redirect_uri=http%3A%2F%2Flocalhost%3A8080%2FLXtory_Auth%2F", "http://127.0.0.1:8080/");
                    //if (authCode != string.Empty)
                    if(authCode.Get("code") != null)
                    {
                        // finished
                        //await OAuthHelpers.GetDropboxToken(authCode);
                        await OAuthHelpers.GetDropboxToken(authCode.Get("code"));
                        StatusLabelText = "Authorization complete";
                        BalloonMessage.ShowMessage("Authorization complete", BalloonIcon.Info);
                        LoginButtonTextDropbox = "Logout";
                    }
                    else
                    {
                        StatusLabelText = "Authorization failed";
                    }
                }
                catch (Exception)
                {
                    StatusLabelText = "Authorization failed";
                    BalloonMessage.ShowMessage("Authorization failed", BalloonIcon.Error);
                    //throw;
                }
            }
            else
            {
                settings.dropboxToken = "";
                settings.Save();
                LoginButtonTextDropbox = "Login";
            }
        }

        private async void GyazoLogin()
        {
            if (settings.gyazoToken == string.Empty)
            {
                statusLabelText = StatusLabelText = "Waiting for Authorization..";
                try
                {
                    //string authCode = await GetAuthCode("https://api.gyazo.com/oauth/authorize?response_type=code&client_id=f6f7ea4ac48869d64d585050fb041a9a85b28f531a1a43833028f75a0a3a6183&redirect_uri=http%3A%2F%2Flocalhost%3A8080%2FLXtory_Auth%2F&scope=public");
                    var authCode = await GetAuthCode2("https://api.gyazo.com/oauth/authorize?response_type=code&client_id=f6f7ea4ac48869d64d585050fb041a9a85b28f531a1a43833028f75a0a3a6183&redirect_uri=http%3A%2F%2Flocalhost%3A8080%2FLXtory_Auth%2F&scope=public", "http://127.0.0.1:8080/");
                    //if (authCode != string.Empty)
                    if(authCode.Get("code") != null)
                    {
                        // get access token
                        //await OAuthHelpers.GetGyazoToken(authCode);
                        await OAuthHelpers.GetGyazoToken(authCode.Get("code"));
                        StatusLabelText = "Authorization complete";
                        BalloonMessage.ShowMessage("Authorization complete", BalloonIcon.Info);
                        LoginButtonTextGyazo = "Logout";
                    }
                    else
                    {
                        // auth failed
                        StatusLabelText = "Authorization failed";
                        BalloonMessage.ShowMessage("Authorization failed", BalloonIcon.Error);
                    }
                }
                catch (Exception)
                {
                    StatusLabelText = "Authorization failed";
                    BalloonMessage.ShowMessage("Authorization failed", BalloonIcon.Error);
                }
            }
            else
            {
                settings.gyazoToken = "";
                settings.Save();
                LoginButtonTextGyazo = "Login";
            }
        }

        private async void ImgurLogin()
        {
            if (settings.username == string.Empty && settings.accessToken == string.Empty)
            {
                StatusLabelText = "Waiting for Authorization..";
                try
                {
                    //string authCode = await GetAuthCode("https://api.imgur.com/oauth2/authorize?client_id=83c1c8bf9f4d2b1&response_type=code&state=LXtory");
                    var authCode = await GetAuthCode2("https://api.imgur.com/oauth2/authorize?client_id=83c1c8bf9f4d2b1&response_type=code&state=LXtory", "http://127.0.0.1:8080/");
                    //if (authCode != string.Empty)
                    if(authCode.Get("code") != null)
                    {
                        //get tokens
                        await OAuthHelpers.GetImgurToken(authCode.Get("code"));
                        Username = settings.username;
                        StatusLabelText = "Authorization complete";
                        BalloonMessage.ShowMessage("Authorization complete", BalloonIcon.Info);
                        LoginButtonText = "Logout";
                    }
                    else
                    {
                        // auth failed
                        StatusLabelText = "Authorization failed";
                        BalloonMessage.ShowMessage("Authorization failed", BalloonIcon.Error);
                    }
                }
                catch (Exception e)
                {
                    StatusLabelText = "Authorization failed";
                    BalloonMessage.ShowMessage("Authorization failed", BalloonIcon.Error);
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

        //private static async Task<string> GetAuthCode(string url)
        //{
        //    //IPAddress local = IPAddress.Loopback;
        //    string accesscode = string.Empty;
        //    TcpListener listener = new TcpListener(IPAddress.Loopback, 8080);
        //    listener.Start();
        //    Byte[] bytes = new Byte[512];
        //    bool receiving = true;
        //    //bool found = false;
        //    Process.Start(url);
        //    while(receiving)
        //    {
        //        Console.WriteLine(@"WAITING CONNECTION");
        //        var client = await listener.AcceptTcpClientAsync();
        //        Console.WriteLine(@"CONNECTED");
        //        NetworkStream stream = client.GetStream();
        //        /*while (receiving)
        //        {
        //            var read = await stream.ReadAsync(bytes, 0, bytes.Length);
        //            var data = Encoding.ASCII.GetString(bytes, 0, read);
        //            Console.WriteLine("RECEIVE: " + data);
        //            if (read == 0)
        //            {
        //                Console.WriteLine("END OF STREAM");
        //            }
        //            byte[] msg = Encoding.ASCII.GetBytes(CloseWindowResponse);
        //            stream.Write(msg, 0, msg.Length);
        //            //string msg = stream.ReadAsync();
        //        }*/
        //        int i;
        //        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0 && receiving)
        //        {
        //            var data = Encoding.ASCII.GetString(bytes, 0, i);
        //            if (data.StartsWith("GET /LXtory_Auth/"))
        //            {
        //                receiving = false;
        //                var regex = new Regex(@"code=(.*?) ");
        //                var result = regex.Match(data);
        //                if (result.Success)
        //                {
        //                    accesscode = result.Groups[1].Value;
        //                    byte[] msg = Encoding.ASCII.GetBytes(CloseWindowResponse);
        //                    stream.Write(msg, 0, msg.Length);
        //                }
        //                /*regex = new Regex(@"access_token=(.*?) ");
        //                result = regex.Match(data);
        //                if (result.Success)
        //                {
        //                    accesscode = result.Groups[1].Value;
        //                    byte[] msg = Encoding.ASCII.GetBytes(CloseWindowResponse);
        //                    stream.Write(msg, 0, msg.Length);
        //                }*/
        //            }
        //        }
        //        Console.WriteLine(@"CLOSING CLIENT");
        //        client.Close();
        //    }
        //    listener.Stop();
        //    return accesscode;
        //}

        private static void EnableContextMenu(bool imagesonly)
        {
            RemoveContextMenu();
            List<string> keys;
            if (imagesonly)
            {
                keys = new List<string> {
                    "SOFTWARE\\Classes\\giffile\\shell\\LXtory",
                    "SOFTWARE\\Classes\\jpegfile\\shell\\LXtory",
                    "SOFTWARE\\Classes\\pngfile\\shell\\LXtory",
                    "SOFTWARE\\Classes\\SystemFileAssociations\\image\\shell\\LXtory" };
                AddRegistryEntries(keys);
            }
            else
            {
                keys = new List<string>
                {
                    "SOFTWARE\\Classes\\*\\shell\\LXtory"
                };
                AddRegistryEntries(keys);
            }
        }

        private static void RemoveContextMenu()
        {
            List<string> keys = new List<string> {
                "SOFTWARE\\Classes\\giffile\\shell\\LXtory",
                "SOFTWARE\\Classes\\jpegfile\\shell\\LXtory",
                "SOFTWARE\\Classes\\pngfile\\shell\\LXtory",
                "SOFTWARE\\Classes\\SystemFileAssociations\\image\\shell\\LXtory",
                "SOFTWARE\\Classes\\*\\shell\\LXtory" };
            RemoveRegistryEntries(keys);
        }

        private static void AddRegistryEntries(List<string> keys)
        {
            string executablePath = Assembly.GetExecutingAssembly().Location;

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

        private static void RemoveRegistryEntries(List<string> keys)
        {
            foreach (string key in keys)
            {
                Registry.CurrentUser.DeleteSubKeyTree(key, false);
            }
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        public static string randomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return base64urlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        public static byte[] sha256(string inputStirng)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }
    }
}
