using nQuant;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Prism.Mvvm;
using LXtory.Notifications;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Text.RegularExpressions;

namespace LXtory
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
        //private readonly string datePattern;
        private readonly string filepath;
        private readonly string filename;

        //public Gif(int framerate, int duration, int w, int h, int x, int y, string datePattern)
        public Gif(int framerate, int duration, int w, int h, int x, int y, string filepath, string filename)
        {
            //this.datePattern = datePattern;
            this.filepath = filepath;
            this.filename = filename;
            this.width = w;
            this.height = h;
            this.posX = x;
            this.posY = y;
            this.frameCount = framerate * duration;
            this.delay = 1000 / framerate;
            this.fileindex = 0;
            this.encodingProgress = 0;
            //this.cachedir = Path.Combine(Properties.Settings.Default.filePath, "gif_frames");
            this.cachedir = Path.Combine(filepath, "gif_frames");
            this.frames = new ObservableCollection<GifFrame>();
            ImageBuffer = new BlockingCollection<Image>();
        }

        public int EncodingProgress
        {
            get { return encodingProgress; }
            set { SetProperty(ref encodingProgress, value); }
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
                    img.StreamSource = fs;
                    img.EndInit();
                    img.Freeze();
                    frame.Image = img;
                }
            }
        }

        public void LoadFromCache()
        {
            Regex digit = new Regex(@"\d+", RegexOptions.Compiled);
            //var files = Directory.EnumerateFiles(cachedir, "*.png").OrderBy(filename => Int32.Parse(Path.GetFileNameWithoutExtension(filename)));
            var files = Directory.EnumerateFiles(cachedir, "*.png").OrderBy(x => int.Parse(digit.Match(x).Value));
            foreach (var file in files)
            {
                GifFrame frame = new GifFrame
                {
                    Filepath = file,
                    Name = Path.GetFileNameWithoutExtension(file)
                };
                frames.Add(frame);
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
                    Image img = ScreenCapture.CaptureArea(width, height, posX, posY, Properties.Settings.Default.gifCaptureCursor);
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

        //public Task<string> EncodeGif(GifProgressNotification gpn)
        public Task<bool> EncodeGif(GifProgressNotification gpn)
        {
            return Task.Run(() =>
            {
                //string date = DateTime.Now.ToString(datePattern);
                //int count = 1;
                //string gifname = $"gif_{date}.gif";

                //while (File.Exists(Path.Combine(Properties.Settings.Default.filePath, gifname)))
                //{
                //    gifname = $"gif_{date}({count}).gif";
                //    count++;
                //}
                bool success = true;
                List<string> filePaths = new List<string>();
                foreach (GifFrame frame in frames)
                {
                    if (frame.Selected)
                    {
                        filePaths.Add(frame.Filepath);
                    }
                }

                try
                {
                    //using (var gif = File.OpenWrite(Path.Combine(Properties.Settings.Default.filePath, gifname)))
                    using (var gif = File.OpenWrite(Path.Combine(this.filepath, this.filename)))
                    {
                        using (var encoder = new GifEncoder(gif))
                        {
                            var quantizer = new WuQuantizer();
                            //var histogram = new Histogram();
                            
                            for (int i = 0; i < filePaths.Count; i++)
                            {
                                if (gpn.Cancelled)
                                {
                                    break;
                                }

                                //using (var image = Image.FromStream(new MemoryStream(File.ReadAllBytes(filePaths[i]))))
                                using (var image = new Bitmap(Image.FromStream(new MemoryStream(File.ReadAllBytes(filePaths[i])))))
                                {

                                    //using (var quantImage = quantizer.QuantizeImage(image, 10, 70, histogram, 256))
                                    //using (var quantImage = quantizer.QuantizeImage(new Bitmap(image)))
                                    using (var quantImage = quantizer.QuantizeImage(image))
                                    {
                                        encoder.AddFrame(quantImage, 0, 0, new TimeSpan(0, 0, 0, 0, delay));
                                    }
                                }
                                EncodingProgress = (int)(((i + 1.0) / filePaths.Count) * 100.0);
                            }
                            quantizer = null;
                            filePaths.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

                if (gpn.Cancelled)
                {
                    File.Delete(Path.Combine(this.filepath, this.filename));
                    //gifname = string.Empty;
                    success = false;
                }
                //return gifname;
                return success;
            });
        }
    }
}
