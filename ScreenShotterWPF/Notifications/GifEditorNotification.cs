using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;

namespace ScreenShotterWPF.Notifications
{
    class GifEditorNotification : Confirmation
    {
        public List<string> Frames { get; set; }
        public List<int> SelectedIndexes { get; set; }

        public GifEditorNotification()
        {
            Frames = new List<string>();
            SelectedIndexes = new List<int>();
        }
    }
}
