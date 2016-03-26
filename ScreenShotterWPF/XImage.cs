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

        private ICommand openLocalCommand;
        private ICommand openBrowserCommand;
        private ICommand copyClipboardCommand;
        [XmlIgnore]
        private bool openLocalEnabled;
        [XmlIgnore]
        private bool openBrowserEnabled;
        [XmlIgnore]
        private bool copyClipboardEnabled;

        public string filename { get; set; }
        public string date { get; set; }
        public DateTime datetime { get; set; }
        [XmlIgnore]
        public byte[] image;
        [XmlIgnore]
        public bool anonupload { get; set; }
        public string url
        {
            get { return this._url; }
            set
            {
                this._url = value;
                OnPropertyChanged("url");
            }
        }
        public string filepath { get; set; }

        public XImage()
        {
            datetime = DateTime.MinValue;
            openLocalEnabled = false;
            openBrowserEnabled = false;
            copyClipboardEnabled = false;
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
            get { return openLocalEnabled; }
            set { openLocalEnabled = value; OnPropertyChanged("OpenLocalEnabled"); }
        }
        [XmlIgnore]
        public bool OpenBrowserEnabled
        {
            get { return openBrowserEnabled; }
            set { openBrowserEnabled = value; OnPropertyChanged("OpenBrowserEnabled"); }
        }
        [XmlIgnore]
        public bool CopyClipboardEnabled
        {
            get { return copyClipboardEnabled; }
            set { copyClipboardEnabled = value; OnPropertyChanged("CopyClipboardEnabled"); }
        }

        private void OpenInBrowser()
        {
            Process.Start(this.url);
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
            catch (Exception ex)
            {
                //RetryClipboard(x.url);
            }
        }
    }
}
