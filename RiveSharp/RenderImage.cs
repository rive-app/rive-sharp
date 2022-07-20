using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RiveSharp
{
    public class RenderImage
    {
        static RenderImage() => InitNativeDelegates();

        SKImage mSKImage;

        public static RenderImage Decode(byte[] data)
        {
            var skimage = SKImage.FromEncodedData(data);
            return skimage != null ? new RenderImage(skimage) : null;
        }

        private RenderImage(SKImage skimage)
        {
            mSKImage = skimage;
        }

        public SKImage SKImage { get { return mSKImage; } }
        public int Width { get { return mSKImage.Width; } }
        public int Height { get { return mSKImage.Height; } }

        // Native interop.
        private delegate void NativeFreeDelegate(IntPtr ptr);
        private delegate int NativeWidthHeightDelegate(IntPtr ptr);

        internal static RenderImage FromNative(IntPtr ptr)
        {
            return (RenderImage)GCHandle.FromIntPtr(ptr).Target;
        }

        internal static void InitNativeDelegates()
        {
            RenderImage_NativeInitDelegates(
                (IntPtr ptr) => GCHandle.FromIntPtr(ptr).Free(),
                (IntPtr ptr) => FromNative(ptr).Width,
                (IntPtr ptr) => FromNative(ptr).Height);
        }

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void RenderImage_NativeInitDelegates(NativeFreeDelegate b,
                                                                   NativeWidthHeightDelegate c,
                                                                   NativeWidthHeightDelegate d);
    }
}
