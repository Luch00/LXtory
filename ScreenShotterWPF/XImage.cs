using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Prism.Commands;
using Prism.Mvvm;

namespace ScreenShotterWPF
{
    // Class for storing image information
    public class XImage : BindableBase
    {
        private string _url;
        private string _thumbnail;

        private ICommand openLocalCommand;
        private ICommand openBrowserCommand;
        private ICommand copyClipboardCommand;

        public string filename { get; set; }
        public string date { get; set; }
        public DateTime datetime { get; set; }
        [XmlIgnore]
        public byte[] image;
        //[XmlIgnore]
        //public bool Anonupload { get; set; }
        [XmlIgnore]
        public UploadSite Uploadsite { get; set; }
        [XmlIgnore]
        public bool IsImage { get; set; } = true;
        public string url
        {
            get { return this._url; }
            set
            {
                this._url = value;
                //OnPropertyChanged("url");
                RaisePropertyChanged("url");
            }
        }
        public string thumbnail
        {
            get { return _thumbnail; }
            set { _thumbnail = value; }
        }
        public string filepath { get; set; }

        public XImage()
        {
            datetime = DateTime.MinValue;
            _thumbnail = "";
            openLocalCommand = new DelegateCommand(OpenLocalImage);
            openBrowserCommand = new DelegateCommand(OpenInBrowser);
            copyClipboardCommand = new DelegateCommand(CopyToClipboard);
        }

        [XmlIgnore]
        public ICommand CopyClipboardCommand
        {
            get { return copyClipboardCommand; }
            set { copyClipboardCommand = value; }
        }
        [XmlIgnore]
        public ICommand OpenBrowserCommand
        {
            get { return openBrowserCommand; }
            set { openBrowserCommand = value; }
        }
        [XmlIgnore]
        public ICommand OpenLocalCommand
        {
            get { return openLocalCommand; }
            set { openLocalCommand = value; }
        }
        [XmlIgnore]
        public bool OpenLocalEnabled
        {
            get { return filepath != string.Empty && System.IO.File.Exists(filepath); }
        }
        [XmlIgnore]
        public bool BrowserAndClipboardEnabled
        {
            get { return url != string.Empty; }
        }

        private void OpenInBrowser()
        {
            try
            {
                Process.Start(this.url);
            }
            catch (Exception)
            {
            }
        }

        private void OpenLocalImage()
        {
            Process.Start(this.filepath);
        }

        private void CopyToClipboard()
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetDataObject(this.url);
            }
            catch (Exception)
            {
                //RetryClipboard(x.url);
            }
        }
    }
}
