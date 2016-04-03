using System.Windows;
using Prism.Interactivity.InteractionRequest;

namespace ScreenShotterWPF.Notifications
{
    class OverlayNotification : Confirmation
    {
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public double WindowTop { get; set; }
        public double WindowLeft { get; set; }
        public Rect Rect { get; set; }

        public OverlayNotification()
        {
            
        }
    }
}
