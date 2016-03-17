using SharpDX.DXGI;
using SharpDX;
using System;
using System.Collections.Generic;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using Rectangle = SharpDX.Rectangle;
using System.IO;

namespace ScreenShotterWPF
{
    internal static class DesktopDuplication
    {
        private static Factory1 factory;
        private static SharpDX.Direct3D11.Device device;
        private static Adapter1 adapter;

        private static void InitDevice()
        {
            // Create DXGI Factory1
            factory = new Factory1();
            adapter = factory.GetAdapter1(0);

            // Create device from Adapter
            device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_1);
        }

        public static byte[] DuplicatePrimaryScreen()
        {
            if (device == null)
            {
                InitDevice();
            }

            Output output = adapter.GetOutput(0);
            using (Bitmap bitmap = GetScreenBitmap(output))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                    //return null;
                }
            }
        }

        public static byte[] DuplicateAllScreens()
        {
            if (device == null)
            {
                InitDevice();
            }

            Output[] outputs = adapter.Outputs;
            List<Rectangle> desktopRects = new List<Rectangle>();
            List<Bitmap> desktopBitmaps = new List<Bitmap>();
            for (int i = 0; i < outputs.Length; i++)
            {
                desktopRects.Add(outputs[i].Description.DesktopBounds);
                desktopBitmaps.Add(GetScreenBitmap(outputs[i]));
            }

            int desktopWidth = (int)SystemParameters.VirtualScreenWidth;
            int desktopHeight = (int)SystemParameters.VirtualScreenHeight;

            int yMin, xMin;
            yMin = xMin = 0;
            foreach (var r in desktopRects)
            {
                if (r.X < xMin)
                {
                    xMin = r.X;
                }
                if (r.Y < yMin)
                {
                    yMin = r.Y;
                }
            }
            using (Bitmap bitmap = new Bitmap(desktopWidth, desktopHeight))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    for (int i = 0; i < desktopBitmaps.Count; i++)
                    {
                        g.DrawImage(desktopBitmaps[i], desktopRects[i].Left + Math.Abs(xMin), desktopRects[i].Top + Math.Abs(yMin));
                    }
                }
                foreach (var item in desktopBitmaps)
                {
                    item.Dispose();
                }
                desktopBitmaps.Clear();
                desktopRects.Clear();
                outputs = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        private static Bitmap GetScreenBitmap(Output output)
        {
            // # of graphics card adapter
            //const int numAdapter = 0;
            
            // Get DXGI.Output
            var output1 = output.QueryInterface<Output1>();
            
            // Width/Height of desktop to capture
            int width = ((Rectangle)output.Description.DesktopBounds).Width;
            int height = ((Rectangle)output.Description.DesktopBounds).Height;

            // Create Staging texture CPU-accessible
            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            var screenTexture = new Texture2D(device, textureDesc);
            
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            // Duplicate the output
            var duplicatedOutput = output1.DuplicateOutput(device);

            bool captureDone = false;
            for (int j = 0; !captureDone; j++)
            {
                try
                {
                    SharpDX.DXGI.Resource screenResource;
                    OutputDuplicateFrameInformation duplicateFrameInformation;
                    
                    // Try to get duplicated frame within given time
                    duplicatedOutput.AcquireNextFrame(10000, out duplicateFrameInformation, out screenResource);

                    if (j > 0)
                    {
                        // copy resource into memory that can be accessed by the CPU
                        using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                            device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                        // Get the desktop capture texture
                        var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                        // Create Drawing.Bitmap
                        
                        var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);
                        
                        // Copy pixels from screen capture Texture to GDI bitmap
                        var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var sourcePtr = mapSource.DataPointer;
                        var destPtr = mapDest.Scan0;
                        for (int y = 0; y < height; y++)
                        {
                            // Copy a single line 
                            Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                            // Advance pointers
                            sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                            destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                        }

                        // Release source and dest locks
                        bitmap.UnlockBits(mapDest);
                        device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                        // Capture done
                        captureDone = true;
                    }

                    screenResource.Dispose();
                    duplicatedOutput.ReleaseFrame();

                }
                catch (SharpDXException e)
                {
                    if (e.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessLost.Result.Code)
                    {
                        duplicatedOutput.Dispose();
                        duplicatedOutput = output1.DuplicateOutput(device);
                    }
                    else if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                    {
                        throw e;
                    }
                }
            }
            
            duplicatedOutput.Dispose();
            screenTexture.Dispose();
            output1.Dispose();
            output.Dispose();
            return bitmap;
        }

        //public static void Test()
        //{
        //    // # of graphics card adapter
        //    const int numAdapter = 0;

        //    // # of output device (i.e. monitor)
        //    //const int numOutput = 0;

        //    const string outputFileName = "FullscreenTest.png";

        //    if (device == null)
        //    {
        //        // Create DXGI Factory1
        //        factory = new Factory1();
        //        adapter = factory.GetAdapter1(numAdapter);

        //        // Create device from Adapter
        //        device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_1);
        //    }
            
        //    Output[] outputs = adapter.Outputs;
        //    List<Rectangle> rect = new List<Rectangle>();
        //    List<Bitmap> desktopBitmaps = new List<Bitmap>();
        //    int desktopWidth = (int)SystemParameters.VirtualScreenWidth;
        //    int desktopHeight = (int)SystemParameters.VirtualScreenHeight;
        //    for (int i = 0; i < outputs.Length; i++)
        //    {
        //        rect.Add(outputs[i].Description.DesktopBounds);
        //        Console.WriteLine("X: " + rect[i].Location.X + ", Y: " + rect[i].Location.Y);

        //        //Console.WriteLine("Left:" + SystemParameters.VirtualScreenLeft + "Top: " + SystemParameters.VirtualScreenTop + ", Width: " + SystemParameters.VirtualScreenWidth + ", Height: " + SystemParameters.VirtualScreenHeight);
        //        // Get DXGI.Output
        //        var output1 = outputs[i].QueryInterface<Output1>();
                
        //        // Width/Height of desktop to capture
        //        int width = outputs[i].Description.DesktopBounds.Width;
        //        int height = outputs[i].Description.DesktopBounds.Height;

        //        // Create Staging texture CPU-accessible
        //        var textureDesc = new Texture2DDescription
        //        {
        //            CpuAccessFlags = CpuAccessFlags.Read,
        //            BindFlags = BindFlags.None,
        //            Format = Format.B8G8R8A8_UNorm,
        //            Width = width,
        //            Height = height,
        //            OptionFlags = ResourceOptionFlags.None,
        //            MipLevels = 1,
        //            ArraySize = 1,
        //            SampleDescription = { Count = 1, Quality = 0 },
        //            Usage = ResourceUsage.Staging
        //        };
        //        var screenTexture = new Texture2D(device, textureDesc);

        //        // Duplicate the output
        //        var duplicatedOutput = output1.DuplicateOutput(device);

        //        bool captureDone = false;
        //        for (int j = 0; !captureDone; j++)
        //        {
        //            try
        //            {
        //                SharpDX.DXGI.Resource screenResource;
        //                OutputDuplicateFrameInformation duplicateFrameInformation;

        //                // Try to get duplicated frame within given time
        //                duplicatedOutput.AcquireNextFrame(10000, out duplicateFrameInformation, out screenResource);

        //                if (j > 0)
        //                {
        //                    // copy resource into memory that can be accessed by the CPU
        //                    using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
        //                        device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

        //                    // Get the desktop capture texture
        //                    var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

        //                    // Create Drawing.Bitmap
        //                    var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        //                    var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);

        //                    // Copy pixels from screen capture Texture to GDI bitmap
        //                    var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
        //                    var sourcePtr = mapSource.DataPointer;
        //                    var destPtr = mapDest.Scan0;
        //                    for (int y = 0; y < height; y++)
        //                    {
        //                        // Copy a single line 
        //                        Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

        //                        // Advance pointers
        //                        sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
        //                        destPtr = IntPtr.Add(destPtr, mapDest.Stride);
        //                    }

        //                    // Release source and dest locks
        //                    bitmap.UnlockBits(mapDest);
        //                    device.ImmediateContext.UnmapSubresource(screenTexture, 0);

        //                    // Save the output
        //                    desktopBitmaps.Add(bitmap);

        //                    // Capture done
        //                    captureDone = true;
        //                }

        //                screenResource.Dispose();
        //                duplicatedOutput.ReleaseFrame();

        //            }
        //            catch (SharpDXException e)
        //            {
        //                if (e.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessLost.Result.Code)
        //                {
        //                    //duplicatedOutput.ReleaseFrame();
        //                    duplicatedOutput.Dispose();
        //                    duplicatedOutput = output1.DuplicateOutput(device);
        //                }
        //                else if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
        //                {
        //                    throw e;
        //                }
        //            }
        //        }
        //        duplicatedOutput.Dispose();
        //    }

        //    int yMin, xMin;
        //    yMin = xMin = 0;
        //    foreach (var r in rect)
        //    {
        //        if (r.Location.X < xMin)
        //        {
        //            xMin = r.Location.X;
        //        }
        //        if (r.Location.Y < yMin)
        //        {
        //            yMin = r.Location.Y;
        //        }
        //    }
        //    Bitmap fullScreen = new Bitmap(desktopWidth, desktopHeight);
        //    using (Graphics g = Graphics.FromImage(fullScreen))
        //    {
        //        for (int i = 0; i < desktopBitmaps.Count; i++)
        //        {
        //            g.DrawImage(desktopBitmaps[i], rect[i].Left + Math.Abs(xMin), rect[i].Top + Math.Abs(yMin));
        //        }
        //    }
        //    fullScreen.Save(outputFileName, ImageFormat.Png);

        //}

        //private static Bitmap getImageFromDXStream(int Width, int Height, DataStream stream)
        //{
        //    var b = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        //    var BoundsRect = new System.Drawing.Rectangle(0, 0, Width, Height);
        //    BitmapData bmpData = b.LockBits(BoundsRect, ImageLockMode.WriteOnly, b.PixelFormat);
        //    int bytes = bmpData.Stride * b.Height;

        //    var rgbValues = new byte[bytes * 4];

        //    // copy bytes from the surface's data stream to the bitmap stream
        //    for (int y = 0; y < Height; y++)
        //    {
        //        for (int x = 0; x < Width; x++)
        //        {
        //            stream.Seek(y * (Width * 4) + x * 4, SeekOrigin.Begin);
        //            stream.Read(rgbValues, y * (Width * 4) + x * 4, 4);
        //        }
        //    }

        //    Marshal.Copy(rgbValues, 0, bmpData.Scan0, bytes);
        //    b.UnlockBits(bmpData);
        //    return b;
        //}

        //private static void SaveImage(Bitmap b)
        //{
        //    b.Save(Path.Combine(Properties.Settings.Default.filePath, "DXImage.png"), ImageFormat.Png);
        //}

    }
}
