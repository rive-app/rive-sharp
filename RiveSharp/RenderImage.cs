// Copyright 2022 Rive

using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace RiveSharp
{
    public class RenderImage
    {
        static RenderImage() => InitNativeDelegates();

        public readonly SKImage SKImage;

        public static RenderImage Decode(byte[] data)
        {
            var skimage = SKImage.FromEncodedData(data);
            return skimage != null ? new RenderImage(skimage) : null;
        }

        private RenderImage(SKImage skimage)
        {
            SKImage = skimage;
        }

        public int Width { get { return SKImage.Width; } }
        public int Height { get { return SKImage.Height; } }

        // Native interop.
        private delegate void NativeFreeDelegate(IntPtr ptr);
        private delegate Int32 NativeWidthHeightDelegate(IntPtr ptr);

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
