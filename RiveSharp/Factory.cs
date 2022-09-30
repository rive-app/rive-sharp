// Copyright 2022 Rive

using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace RiveSharp
{
    public class Factory
    {
        static readonly FactoryDelegates Delegates = new FactoryDelegates
        {
            Release = RiveAPI.ReleaseNativeRefCallback,
            MakeRenderPath = MakeRenderPathCallback,
            MakeEmptyRenderPath = MakeEmptyRenderPathCallback,
            MakeRenderPaint = MakeRenderPaintCallback,
            DecodeImage = DecodeImageCallback
        };

        static Factory()
        {
            RiveAPI.Factory_RegisterDelegates(Delegates);
        }

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

        [MonoPInvokeCallback(typeof(FactoryDelegates.MakeRenderPathDelegate))]
        static IntPtr MakeRenderPathCallback(IntPtr @ref,
                                             IntPtr ptsArray,  // SKPoint[nPts]
                                             int nPts,
                                             IntPtr verbsArray,  // byte[nVerbs]
                                             int nVerbs,
                                             int fillRule)
        {
            var factory = RiveAPI.CastNativeRef<Factory>(@ref);
            var pts = new SKPoint[nPts];
            RiveAPI.CopySKPointArray(ptsArray, pts, nPts);
            var verbs = new byte[nVerbs];
            Marshal.Copy(verbsArray, verbs, 0, nVerbs);
            return RiveAPI.CreateNativeRef(factory.MakeRenderPath(pts, verbs, (FillRule)fillRule));
        }

        [MonoPInvokeCallback(typeof(FactoryDelegates.MakeEmptyObjectDelegate))]
        static IntPtr MakeEmptyRenderPathCallback(IntPtr @ref)
        {
            var factory = RiveAPI.CastNativeRef<Factory>(@ref);
            return RiveAPI.CreateNativeRef(factory.MakeEmptyRenderPath());
        }

        [MonoPInvokeCallback(typeof(FactoryDelegates.MakeEmptyObjectDelegate))]
        static IntPtr MakeRenderPaintCallback(IntPtr @ref)
        {
            var factory = RiveAPI.CastNativeRef<Factory>(@ref);
            return RiveAPI.CreateNativeRef(factory.MakeRenderPaint());
        }

        [MonoPInvokeCallback(typeof(FactoryDelegates.DecodeImageDelegate))]
        static IntPtr DecodeImageCallback(IntPtr @ref, IntPtr bytesArray, int nBytes)
        {
            var factory = RiveAPI.CastNativeRef<Factory>(@ref);
            var bytes = new byte[nBytes];
            Marshal.Copy(bytesArray, bytes, 0, nBytes);
            var image = factory.DecodeImage(bytes);
            return (IntPtr)(image != null ? RiveAPI.CreateNativeRef(image)
                                          : IntPtr.Zero);
        }
    }
}
