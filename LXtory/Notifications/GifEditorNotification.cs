using Prism.Interactivity.InteractionRequest;

namespace LXtory.Notifications
{
    class GifEditorNotification : Confirmation
    {
        public Gif Gif { get; set; }

        public GifEditorNotification()
        {
        }
    }
}
