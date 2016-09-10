using System.Collections.Generic;

namespace ScreenShotterWPF
{
    internal static class ImageFileTypes
    {
        private static readonly List<string> supportedTypes = new List<string> { ".jpg", ".jpeg", ".bmp", ".gif", ".png" };

        public static IEnumerable<string> SupportedTypes
        {
            get { return supportedTypes;}
        }

    }

    public enum UploadSite
    {
        Imgur = 0,
        Gyazo = 1,
        Puush = 2,
        SFTP = 3,
        None = 99
    }
}
