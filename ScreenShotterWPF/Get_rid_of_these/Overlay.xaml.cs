using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenShotterWPF
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class Overlay
    {
        public Point start {get; set;}
        public Point end { get; set; }
        Rectangle r;
        TextBlock textBlock;
        public Overlay()
        {
            InitializeComponent();
        }

        private void PaintSurface_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released || r == null)
                return;

            var pos = e.GetPosition(this);
            
            var x = Math.Min(pos.X, start.X);
            var y = Math.Min(pos.Y, start.Y);

            var w = Math.Max(pos.X, start.X) - x;
            var h = Math.Max(pos.Y, start.Y) - y;

            r.Width = w;
            r.Height = h;
            Canvas.SetLeft(r, x);
            Canvas.SetTop(r, y);
            if (w > 1)
            {
                textBlock.Visibility = Visibility.Visible;
                SetText(x, y, w, h);
            }
            else
            {
                textBlock.Visibility = Visibility.Hidden;
            }
        }

        private void SetText(double x, double y, double w, double h)
        {
            textBlock.Text = $"H: {h}\nW: {w}";
            double xpos = x + w - 50;
            double ypos = y + h - 35;
            Canvas.SetLeft(textBlock, xpos);
            Canvas.SetTop(textBlock, ypos);
        }

        private void PaintSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                start = e.GetPosition(PaintSurface);
                r = new Rectangle
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(r, start.X);
                Canvas.SetTop(r, start.Y);
                PaintSurface.Children.Add(r);
                textBlock = new TextBlock
                {
                    Foreground = Brushes.Red,
                    Visibility = Visibility.Hidden
                };
                PaintSurface.Children.Add(textBlock);
            }
        }

        private void PaintSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                end = e.GetPosition(PaintSurface);
                r = null;
                PaintSurface.Children.Clear();
                this.Opacity = 0;
                DialogResult = true;
            }
        }

        private void PaintSurface_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }
    }
}
