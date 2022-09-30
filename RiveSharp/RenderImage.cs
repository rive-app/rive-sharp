// Copyright 2022 Rive

using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace RiveSharp
{
    public class RenderImage
    {
        static readonly RenderImageDelegates Delegates = new RenderImageDelegates
        {
            Release = RiveAPI.ReleaseNativeRefCallback,
            Width = WidthCallback,
            Height = HeightCallback
        };

        static RenderImage()
        {
            RiveAPI.RenderImage_RegisterDelegates(Delegates);
        }

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

        public int Width => SKImage.Width;
        public int Height => SKImage.Height;

        [MonoPInvokeCallback(typeof(RenderImageDelegates.WidthHeightDelegate))]
        static Int32 WidthCallback(IntPtr @ref)
        {
            var renderImage = RiveAPI.CastNativeRef<RenderImage>(@ref);
            return renderImage.Width;
        }

        [MonoPInvokeCallback(typeof(RenderImageDelegates.WidthHeightDelegate))]
        static Int32 HeightCallback(IntPtr @ref)
        {
            var renderImage = RiveAPI.CastNativeRef<RenderImage>(@ref);
            return renderImage.Height;
        }
    }
}
