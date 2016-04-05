using nQuant;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Prism.Mvvm;
using ScreenShotterWPF.Notifications;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace ScreenShotterWPF
{
    internal class Gif : BindableBase
    {
        private readonly BlockingCollection<Image> ImageBuffer;
        private readonly int delay;
        private readonly int frameCount;
        private readonly int width;
        private readonly int height;
        private readonly int posX;
        private readonly int posY;
        private int fileindex;
        private readonly string cachedir;
        private readonly ObservableCollection<GifFrame> frames;
        private int encodingProgress;

        public Gif(int framerate, int duration, int w, int h, int x, int y)
        {
            this.width = w;
            this.height = h;
            this.posX = x;
            this.posY = y;
            this.frameCount = framerate * duration;
            this.delay = 1000 / framerate;
            this.fileindex = 0;
            this.encodingProgress = 0;
            this.cachedir = Path.Combine(Properties.Settings.Default.filePath, "gif_frames");
            this.frames = new ObservableCollection<GifFrame>();
            ImageBuffer = new BlockingCollection<Image>();
        }

        public int EncodingProgress
        {
            get { return encodingProgress; }
            set { encodingProgress = value; OnPropertyChanged("EncodingProgress"); }
        }

        public ObservableCollection<GifFrame> Frames
        {
            get { return frames; }
        }

        public void LoadThumbnails()
        {
            foreach (GifFrame frame in frames)
            {
                using (FileStream fs = new FileStream(frame.Filepath, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = 300;
                    //img.UriSource = new Uri(s, UriKind.Absolute);
                    img.StreamSource = fs;
                    img.EndInit();
                    img.Freeze();
                    frame.Image = img;
                }
            }
        }
        
        public async Task StartCapture()
        {
            var cap = Capture();
            await BufferToDisk();
        }

        private Task Capture()
        {
            return Task.Run(() => 
            {
                for (int i = 0; i < this.frameCount; i++)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    Image img = CaptureArea(width, height, posX, posY);
                    ImageBuffer.Add(img);
                    if (i + 1 < frameCount)
                    {
                        int sleep = delay - (int)sw.ElapsedMilliseconds;
                        if (sleep > 0)
                        {
                            Thread.Sleep(sleep);
                        }
                    }
                }
                ImageBuffer.CompleteAdding();
            });
        }

        private Task BufferToDisk()
        {
            if (!Directory.Exists(cachedir))
            {
                Directory.CreateDirectory(cachedir);
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(cachedir);
                foreach (FileInfo file in dir.GetFiles())
                {
                    if (file.Extension.ToLowerInvariant() == ".png")
                    {
                        file.Delete();
                    }
                }
            }

            return Task.Run(() => 
            {
                while (!ImageBuffer.IsCompleted)
                {
                    Image img = null;
                    try
                    {
                        img = ImageBuffer.Take();
                        if (img != null)
                        {
                            string f = Path.Combine(cachedir, $"frame{fileindex}.png");
                            img.Save(f, ImageFormat.Png);
                            GifFrame frame = new GifFrame
                            {
                                Filepath = f,
                                Name = $"Frame{fileindex}"
                            };
                            frames.Add(frame);
                            fileindex++;
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        img?.Dispose();
                    }
                }
            });
        }

        private static Image CaptureArea(int w, int h, int x, int y)
        {
            Image img = new Bitmap(w, h, PixelFormat.Format32bppRgb);
            using (var gfx = Graphics.FromImage(img))
            {
                gfx.CopyFromScreen(x,
                                    y,
                                    0,
                                    0,
                                    new Size(w, h),
                                    CopyPixelOperation.SourceCopy);
                if (Properties.Settings.Default.gifCaptureCursor)
                {
                    NativeMethods.CURSORINFO pci;
                    pci.cbSize = Marshal.SizeOf(typeof(NativeMethods.CURSORINFO));

                    if (NativeMethods.GetCursorInfo(out pci))
                    {
                        if (pci.flags == NativeMethods.CURSOR_SHOWING)
                        {
                            var hdc = gfx.GetHdc();
                            NativeMethods.DrawIconEx(hdc, pci.ptScreenPos.x - x, pci.ptScreenPos.y - y, pci.hCursor, 0, 0, 0, IntPtr.Zero, NativeMethods.DI_NORMAL);
                            gfx.ReleaseHdc();
                        }
                    }
                }
            }
            return img;
        }

        public Task<string> EncodeGif(GifProgressNotification gpn)
        {
            return Task.Run(() =>
            {
                const string datePattern = @"dd-MM-yy_HH-mm-ss";
                string date = DateTime.Now.ToString(datePattern);
                int count = 1;
                string gifname = $"gif_{date}.gif";

                while (File.Exists(Path.Combine(Properties.Settings.Default.filePath, gifname)))
                {
                    gifname = $"gif_{date}({count}).gif";
                    count++;
                }

                List<string> filePaths = new List<string>();
                foreach (GifFrame frame in frames)
                {
                    if (frame.Selected)
                    {
                        filePaths.Add(frame.Filepath);
                    }
                }
                Console.WriteLine("SELECTED: " + filePaths.Count);
                try
                {
                    using (var gif = File.OpenWrite(Path.Combine(Properties.Settings.Default.filePath, gifname)))
                    {
                        using (var encoder = new GifEncoder(gif))
                        {
                            var quantizer = new WuQuantizer();
                            
                            for (int i = 0; i < filePaths.Count; i++)
                            {
                                if (gpn.Cancelled)
                                {
                                    Console.WriteLine("CANCEL REQUESTED");
                                    break;
                                }

                                using (var image = Image.FromStream(new MemoryStream(File.ReadAllBytes(filePaths[i]))))
                                {
                                    using (var quantImage = quantizer.QuantizeImage(new Bitmap(image)))
                                    {
                                        encoder.AddFrame(quantImage, 0, 0, new TimeSpan(0, 0, 0, 0, delay));
                                    }
                                }
                                EncodingProgress = (int)(((i + 1.0) / filePaths.Count) * 100.0);
                            }
                            filePaths.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }

                if (gpn.Cancelled)
                {
                    Console.WriteLine("DELETE UNFINISHED");
                    File.Delete(Path.Combine(Properties.Settings.Default.filePath, gifname));
                    gifname = string.Empty;
                }
                return gifname;
            });
        }
    }
}
