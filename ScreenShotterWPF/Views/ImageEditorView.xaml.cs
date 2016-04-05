using System.Windows.Controls;

namespace ScreenShotterWPF.Views
{
    /// <summary>
    /// Interaction logic for ImageEditor.xaml
    /// </summary>
    public partial class ImageEditorView : UserControl
    {
        public ImageEditorView()
        {
            InitializeComponent();
        }

        //public byte[] editedImage { get; set; }
        //private Point start { get; set; }
        //private Point end { get; set; }
        //private Rectangle r;
        //private Ellipse c;
        //private Line l;
        //private ArrowLine a;

        //public Editor(byte[] backgroundImage)
        //{
        //    InitializeComponent();
        //    toolFreeform.IsChecked = true;
        //    BitmapImage background = ByteArrayToBitmapImage(backgroundImage);
        //    EditorCanvas.MinHeight = background.PixelHeight;
        //    EditorCanvas.MaxHeight = background.PixelHeight;
        //    EditorCanvas.Height = background.PixelHeight;
        //    EditorCanvas.MinWidth = background.PixelWidth;
        //    EditorCanvas.MaxWidth = background.PixelWidth;
        //    EditorCanvas.Width = background.PixelWidth;
        //    EditorCanvas.Background = new ImageBrush(background);
        //    strokeThickness.Value = (int)EditorCanvas.DefaultDrawingAttributes.Width;
        //}

        //private static BitmapImage ByteArrayToBitmapImage(byte[] array)
        //{
        //    BitmapImage img = new BitmapImage();
        //    MemoryStream ms = new MemoryStream(array);
        //    img.BeginInit();
        //    img.StreamSource = ms;
        //    img.EndInit();
        //    return img;
        //}

        //private void confirmMenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        var size = new Size(EditorCanvas.ActualWidth, EditorCanvas.ActualHeight);
        //        EditorCanvas.Margin = new Thickness(0, 0, 0, 0);
        //        EditorCanvas.Measure(size);
        //        EditorCanvas.Arrange(new Rect(size));
        //        //RenderTargetBitmap rtb = new RenderTargetBitmap((int)EditorCanvas.Width, (int)EditorCanvas.Height, 96d, 96d, PixelFormats.Default);
        //        RenderTargetBitmap rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Default);
        //        rtb.Render(EditorCanvas);
        //        PngBitmapEncoder encoder = new PngBitmapEncoder();
        //        encoder.Frames.Add(BitmapFrame.Create(rtb));
        //        encoder.Save(ms);
        //        editedImage = ms.ToArray();
        //        DialogResult = true;
        //    }
        //}

        //private void closeMenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    DialogResult = false;
        //}

        //private void toolRectangle_Click(object sender, RoutedEventArgs e)
        //{
        //    toolFreeform.IsChecked = false;
        //    toolLine.IsChecked = false;
        //    toolEllipse.IsChecked = false;
        //    toolArrow.IsChecked = false;
        //    EditorCanvas.EditingMode = InkCanvasEditingMode.None;
        //}

        //private void toolEllipse_Click(object sender, RoutedEventArgs e)
        //{
        //    toolFreeform.IsChecked = false;
        //    toolLine.IsChecked = false;
        //    toolRectangle.IsChecked = false;
        //    toolArrow.IsChecked = false;
        //    EditorCanvas.EditingMode = InkCanvasEditingMode.None;
        //}

        //private void toolLine_Click(object sender, RoutedEventArgs e)
        //{
        //    toolFreeform.IsChecked = false;
        //    toolEllipse.IsChecked = false;
        //    toolRectangle.IsChecked = false;
        //    toolArrow.IsChecked = false;
        //    EditorCanvas.EditingMode = InkCanvasEditingMode.None;
        //}
        //private void toolArrow_Click(object sender, RoutedEventArgs e)
        //{
        //    toolLine.IsChecked = false;
        //    toolEllipse.IsChecked = false;
        //    toolRectangle.IsChecked = false;
        //    toolFreeform.IsChecked = false;
        //    EditorCanvas.EditingMode = InkCanvasEditingMode.None;
        //}
        //private void toolFreeform_Click(object sender, RoutedEventArgs e)
        //{
        //    toolLine.IsChecked = false;
        //    toolEllipse.IsChecked = false;
        //    toolRectangle.IsChecked = false;
        //    toolArrow.IsChecked = false;
        //    EditorCanvas.EditingMode = InkCanvasEditingMode.Ink;
        //}

        //private void EditorCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (toolRectangle.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Pressed)
        //        {
        //            start = e.GetPosition(EditorCanvas);
        //            r = new Rectangle
        //            {
        //                Stroke = new SolidColorBrush(EditorCanvas.DefaultDrawingAttributes.Color),
        //                StrokeThickness = 2
        //            };
        //            InkCanvas.SetLeft(r, start.X);
        //            InkCanvas.SetTop(r, start.Y);
        //            EditorCanvas.Children.Add(r);
        //        }
        //    }
        //    if (toolEllipse.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Pressed)
        //        {
        //            start = e.GetPosition(EditorCanvas);
        //            c = new Ellipse
        //            {
        //                Stroke = new SolidColorBrush(EditorCanvas.DefaultDrawingAttributes.Color),
        //                StrokeThickness = 2
        //            };
        //            InkCanvas.SetLeft(c, start.X);
        //            InkCanvas.SetTop(c, start.Y);
        //            EditorCanvas.Children.Add(c);
        //        }
        //    }
        //    if (toolLine.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Pressed)
        //        {
        //            start = e.GetPosition(EditorCanvas);
        //            l = new Line
        //            {
        //                Stroke = new SolidColorBrush(EditorCanvas.DefaultDrawingAttributes.Color),
        //                StrokeThickness = 2,
        //                X1 = start.X,
        //                Y1 = start.Y
        //            };
        //            EditorCanvas.Children.Add(l);
        //        }
        //    }
        //    if (toolArrow.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Pressed)
        //        {
        //            start = e.GetPosition(EditorCanvas);
        //            a = new ArrowLine
        //            {
        //                Stroke = new SolidColorBrush(EditorCanvas.DefaultDrawingAttributes.Color),
        //                StrokeThickness = EditorCanvas.DefaultDrawingAttributes.Width,
        //                IsArrowClosed = true,
        //                Fill = new SolidColorBrush(EditorCanvas.DefaultDrawingAttributes.Color),
        //                ArrowLength = 20,
        //                X1 = start.X,
        //                Y1 = start.Y
        //            };
        //            EditorCanvas.Children.Add(a);
        //        }
        //    }
        //}

        //private void EditorCanvas_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (toolRectangle.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Released || r == null)
        //            return;

        //        var pos = e.GetPosition(EditorCanvas);

        //        var x = Math.Min(pos.X, start.X);
        //        var y = Math.Min(pos.Y, start.Y);

        //        var w = Math.Max(pos.X, start.X) - x;
        //        var h = Math.Max(pos.Y, start.Y) - y;

        //        r.Width = w;
        //        r.Height = h;
        //        InkCanvas.SetLeft(r, x);
        //        InkCanvas.SetTop(r, y);
        //    }
        //    if (toolEllipse.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Released || c == null)
        //            return;

        //        var pos = e.GetPosition(EditorCanvas);

        //        var x = Math.Min(pos.X, start.X);
        //        var y = Math.Min(pos.Y, start.Y);

        //        var w = Math.Max(pos.X, start.X) - x;
        //        var h = Math.Max(pos.Y, start.Y) - y;

        //        c.Width = w;
        //        c.Height = h;
        //        InkCanvas.SetLeft(c, x);
        //        InkCanvas.SetTop(c, y);
        //    }
        //    if (toolLine.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Released || l == null)
        //            return;

        //        var pos = e.GetPosition(EditorCanvas);

        //        l.X2 = pos.X;
        //        l.Y2 = pos.Y;
        //    }
        //    if (toolArrow.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Released || a == null)
        //            return;

        //        var pos = e.GetPosition(EditorCanvas);

        //        a.X2 = pos.X;
        //        a.Y2 = pos.Y;
        //    }
        //}

        //private void EditorCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    if (toolRectangle.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Released)
        //        {
        //            end = e.GetPosition(EditorCanvas);
        //            r = null;
        //        }
        //    }
        //    if (toolEllipse.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Released)
        //        {
        //            end = e.GetPosition(EditorCanvas);
        //            c = null;
        //        }
        //    }
        //    if (toolLine.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Released)
        //        {
        //            end = e.GetPosition(EditorCanvas);
        //            l = null;
        //        }
        //    }
        //    if (toolArrow.IsChecked)
        //    {
        //        if (e.LeftButton == MouseButtonState.Released)
        //        {
        //            end = e.GetPosition(EditorCanvas);
        //            a = null;
        //        }
        //    }
        //}

        //private void colorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        //{
        //    EditorCanvas.DefaultDrawingAttributes.Color = e?.NewValue ?? Colors.Red;
        //    //EditorCanvas.DefaultDrawingAttributes.Color = e.NewValue.Value;
        //    //e?.NewValue
        //}

        //private void strokeThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    EditorCanvas.DefaultDrawingAttributes.Width = (int)e.NewValue;
        //    EditorCanvas.DefaultDrawingAttributes.Height = (int)e.NewValue;
        //}

        //private void clearEdits_Click(object sender, RoutedEventArgs e)
        //{
        //    EditorCanvas.Strokes.Clear();
        //    EditorCanvas.Children.Clear();
        //}
    }
}
