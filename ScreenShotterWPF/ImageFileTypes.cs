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
}
