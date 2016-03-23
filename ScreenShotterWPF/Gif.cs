using nQuant;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ScreenShotterWPF.Notifications;

namespace ScreenShotterWPF
{
    internal class Gif : INotifyPropertyChanged
    {
        /// 1. Get framerate and record length
        /// 2. calculate frame count and delay
        /// 3. Get display area and start capturing
        /// 4. Captured images to BlockingCollection and start saving on disk
        /// 5. once capture / saving is finished start making into gif
        /// 6. save local / upload gif & delete single frames

        private readonly BlockingCollection<Image> ImageBuffer;
        private readonly int delay;
        private readonly int frameCount;
        private readonly int width;
        private readonly int height;
        private readonly int posX;
        private readonly int posY;
        private int filename;
        private readonly string cachedir;
        private readonly List<string> frames;
        private int encodingProgress;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Gif(int framerate, int duration, int w, int h, int x, int y)
        {
            this.width = w;
            this.height = h;
            this.posX = x;
            this.posY = y;
            //this.duration = duration;
            //this.frameRate = framerate;
            this.frameCount = framerate * duration;
            this.delay = 1000 / framerate;
            this.filename = 0;
            this.cachedir = Path.Combine(Properties.Settings.Default.filePath, "gif_frames");
            this.frames = new List<string>();
            ImageBuffer = new BlockingCollection<Image>();
        }

        public int EncodingProgress
        {
            get { return encodingProgress; }
            set { encodingProgress = value; RaisePropertyChanged("EncodingProgress"); }
        }

        public async Task<List<string>> StartCapture()
        {
            var cap = Capture();
            await BufferToDisk();
            return frames;
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
                            //string f = cachedir + "\\frame" + filename + ".png"; // RANDOM FILENAME INTO LIST 
                            string f = Path.Combine(cachedir, $"frame{filename}.png");
                            img.Save(f, ImageFormat.Png);
                            frames.Add(f);
                            filename++;
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

        public Task<string> EncodeGif2(List<string> filePaths, GifProgressNotification gpn)
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
                                gpn.Progress = (int)(((i + 1.0) / filePaths.Count) * 100.0);
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

        //public Task<string> EncodeGif(List<string> filePaths, int quality, EncodingProgressViewModel vm)
        //{
        //    return Task.Run(() => 
        //    {
        //        const string datePattern = @"dd-MM-yy_HH-mm-ss";
        //        string date = DateTime.Now.ToString(datePattern);
        //        int count = 1;
        //        string gifname = $"gif_{date}.gif";

        //        while (File.Exists(Path.Combine(Properties.Settings.Default.filePath, gifname)))
        //        {
        //            gifname = $"gif_{date}({count}).gif";
        //            count++;
        //        }

        //        try
        //        {
        //            using (var gif = File.OpenWrite(Path.Combine(Properties.Settings.Default.filePath, gifname)))
        //            {
        //                AnimatedGifEncoder e = new AnimatedGifEncoder();
        //                e.SetRepeat(0);
        //                e.SetQuality(quality);
        //                e.Start(gif);
        //                for (int i = 0; i < filePaths.Count; i++)
        //                {
        //                    if (vm.CancelRequested)
        //                    {
        //                        Console.WriteLine("CANCEL REQUESTED");
        //                        e.Finish();
        //                        gif.Close();
        //                        break;
        //                    }
        //                    using (var image = Image.FromStream(new MemoryStream(File.ReadAllBytes(filePaths[i]))))
        //                    {
        //                        e.AddFrame(image);
        //                    }
        //                    e.SetDelay(delay);
        //                    vm.ProgressValue = (int)(((i + 1.0) / filePaths.Count) * 100.0);
        //                }
        //                e.Finish();
        //                gif.Close();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //            throw ex;
        //        }

        //        if (vm.CancelRequested)
        //        {
        //            Console.WriteLine("DELETE UNFINISHED");
        //            File.Delete(Path.Combine(Properties.Settings.Default.filePath, gifname));
        //            gifname = string.Empty;
        //        }

        //        /*using (MagickImageCollection c = new MagickImageCollection())
        //        {
        //            foreach (string s in filePaths)
        //            {
        //                c.Add(s);
        //                c[c.Count-1].AnimationDelay = delay / 10;
        //            }
        //            //c.Optimize();
        //            c.Write(Path.Combine(Properties.Settings.Default.filePath, gifname));
        //        }*/

        //        /*using (var gif = File.OpenWrite(Path.Combine(Properties.Settings.Default.filePath, gifname)))
        //        {
        //            using (var encoder = new GifEncoder(gif))
        //            {
        //                foreach (string f in filePaths)
        //                {
        //                    using (var image = Image.FromStream(new MemoryStream(File.ReadAllBytes(f))))
        //                    {   
        //                        encoder.AddFrame(image, 0, 0, new TimeSpan(0, 0, 0, 0, delay));
        //                    }
        //                }
        //                filePaths.Clear();
        //            }
        //        }*/
        //        return gifname;
        //    });
        //}
    }
}
