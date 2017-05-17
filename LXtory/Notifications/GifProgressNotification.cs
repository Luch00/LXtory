using Prism.Interactivity.InteractionRequest;

namespace LXtory.Notifications
{
    class GifProgressNotification : Confirmation
    {
        public Gif Gif { get; set; }
        public bool Cancelled { get; set; }
        public string Name { get; set; }

        public GifProgressNotification()
        {
            Cancelled = false;
        }
    }
}
