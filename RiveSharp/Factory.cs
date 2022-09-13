// Copyright 2022 Rive

using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace RiveSharp
{
    public class Factory
    {
        static Factory() => InitNativeDelegates();
        public static readonly Factory Instance = new Factory();
        private Factory() { }

        RenderPath MakeRenderPath(SKPoint[] pts, byte[] verbs, FillRule fillRule)
        {
            return new RenderPath(pts, verbs, fillRule);
        }

        RenderPath MakeEmptyRenderPath()
        {
            return new RenderPath();
        }

        RenderPaint MakeRenderPaint()
        {
            return new RenderPaint();
        }

        RenderImage DecodeImage(byte[] data)
        {
            return RenderImage.Decode(data);
        }

        // Native interop.
        private delegate void NativeFreeDelegate(IntPtr ptr);
        private delegate IntPtr NativeMakeRenderPathDelegate(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)]
            SKPoint[] pts,
            int nPts,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=4)]
            byte[] verbs,
            int nVerbs,
            int fillRule);
        private delegate IntPtr NativeMakeEmptyObjectDelegate(IntPtr ptr);
        private delegate IntPtr NativeDecodeImageDelegate(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)]
            byte[] data,
            int n);

        internal IntPtr RefNative()
        {
            return GCHandle.ToIntPtr(GCHandle.Alloc(this));
        }

        internal static Factory FromNative(IntPtr ptr)
        {
            return (Factory)GCHandle.FromIntPtr(ptr).Target;
        }

        internal static void InitNativeDelegates()
        {
            Factory_NativeInitDelegates(
                (IntPtr ptr) => GCHandle.FromIntPtr(ptr).Free(),
                (IntPtr ptr, SKPoint[] pts, int nPts, byte[] verbs, int nVerbs, int fillRule) =>
                    GCHandle.ToIntPtr(GCHandle.Alloc(FromNative(ptr).MakeRenderPath(pts, verbs, (FillRule)fillRule))),
                (IntPtr ptr) => GCHandle.ToIntPtr(GCHandle.Alloc(FromNative(ptr).MakeEmptyRenderPath())),
                (IntPtr ptr) => GCHandle.ToIntPtr(GCHandle.Alloc(FromNative(ptr).MakeRenderPaint())),
                (IntPtr ptr, byte[] data, int n) =>
                {
                    var image = FromNative(ptr).DecodeImage(data);
                    return (IntPtr)(image != null ? GCHandle.ToIntPtr(GCHandle.Alloc(image))
                                                  : IntPtr.Zero);
                });
        }

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Factory_NativeInitDelegates(NativeFreeDelegate free,
                                                               NativeMakeRenderPathDelegate makeRenderPath,
                                                               NativeMakeEmptyObjectDelegate makeEmptyRenderPath,
                                                               NativeMakeEmptyObjectDelegate makeRenderPaint,
                                                               NativeDecodeImageDelegate decodeImage);
    }
}
