using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LXtory
{
    internal static class ScreenCapture
    {
        internal static Image CaptureArea(int w, int h, int x, int y, bool captureCursor)
        {
            if (w < 1)
            {
                w = 1;
            }
            if (h < 1)
            {
                h = 1;
            }
            Image img = new Bitmap(w, h, PixelFormat.Format32bppRgb);
            using (var gfx = Graphics.FromImage(img))
            {
                gfx.CopyFromScreen(x,
                                    y,
                                    0,
                                    0,
                                    new Size(w, h),
                                    CopyPixelOperation.SourceCopy);

                // Draw cursor on captured image
                if (captureCursor)
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
    }
}
