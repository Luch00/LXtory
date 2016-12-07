using Prism.Interactivity.InteractionRequest;

namespace ScreenShotterWPF.Notifications
{
    class GifEditorNotification : Confirmation
    {
        public Gif Gif { get; set; }

        public GifEditorNotification()
        {
        }
    }
}
