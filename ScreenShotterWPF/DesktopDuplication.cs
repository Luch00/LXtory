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
    }
}
