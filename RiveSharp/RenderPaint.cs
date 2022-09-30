// Copyright 2022 Rive

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace RiveSharp
{
    public enum RenderPaintStyle
    {
        Stroke = 0,
        Fill = 1
    }

    public enum StrokeJoin
    {
        Miter = 0,
        Round = 1,
        Bevel = 2
    }

    public enum StrokeCap
    {
        Butt = 0,
        Round = 1,
        Square = 2
    }

    public enum BlendMode
    {
        SrcOver = 3,
        Screen = 14,
        Overlay = 15,
        Darken = 16,
        Lighten = 17,
        ColorDodge = 18,
        ColorBurn = 19,
        HardLight = 20,
        SoftLight = 21,
        Difference = 22,
        Exclusion = 23,
        Multiply = 24,
        Hue = 25,
        Saturation = 26,
        Color = 27,
        Luminosity = 28
    }

    public class RenderPaint
    {
        static readonly RenderPaintDelegates Delegates = new RenderPaintDelegates
        {
            Release = RiveAPI.ReleaseNativeRefCallback,
            Style = StyleCallback,
            Color = ColorCallback,
            LinearGradient = LinearGradientCallback,
            RadialGradient = RadialGradientCallback,
            Thickness = ThicknessCallback,
            Join = JoinCallback,
            Cap = CapCallback,
            BlendMode = BlendModeCallback
        };

        static RenderPaint()
        {
            RiveAPI.RenderPaint_RegisterDelegates(Delegates);
        }

        public readonly SKPaint SKPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        private static SKPaintStyle ToSKPaintStyle(RenderPaintStyle style)
        {
            switch (style)
            {
                case RenderPaintStyle.Stroke: return SKPaintStyle.Stroke;
                case RenderPaintStyle.Fill: return SKPaintStyle.Fill;
            }
            throw new InvalidEnumArgumentException("Invalid RenderPaintStyle " + style);
        }

        private static SKStrokeJoin ToSKStrokeJoin(StrokeJoin join)
        {
            switch (join)
            {
                case StrokeJoin.Miter: return SKStrokeJoin.Miter;
                case StrokeJoin.Round: return SKStrokeJoin.Round;
                case StrokeJoin.Bevel: return SKStrokeJoin.Bevel;
            }
            throw new InvalidEnumArgumentException("Invalid StrokeJoin " + join);
        }

        private static SKStrokeCap ToSKStrokeCap(StrokeCap cap)
        {
            switch (cap)
            {
                case StrokeCap.Butt: return SKStrokeCap.Butt;
                case StrokeCap.Round: return SKStrokeCap.Round;
                case StrokeCap.Square: return SKStrokeCap.Square;
            }
            throw new InvalidEnumArgumentException("Invalid StrokeCap " + cap);
        }

        internal static SKBlendMode ToSKBlendMode(BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.SrcOver: return SKBlendMode.SrcOver;
                case BlendMode.Screen: return SKBlendMode.Screen;
                case BlendMode.Overlay: return SKBlendMode.Overlay;
                case BlendMode.Darken: return SKBlendMode.Darken;
                case BlendMode.Lighten: return SKBlendMode.Lighten;
                case BlendMode.ColorDodge: return SKBlendMode.ColorDodge;
                case BlendMode.ColorBurn: return SKBlendMode.ColorBurn;
                case BlendMode.HardLight: return SKBlendMode.HardLight;
                case BlendMode.SoftLight: return SKBlendMode.SoftLight;
                case BlendMode.Difference: return SKBlendMode.Difference;
                case BlendMode.Exclusion: return SKBlendMode.Exclusion;
                case BlendMode.Multiply: return SKBlendMode.Multiply;
                case BlendMode.Hue: return SKBlendMode.Hue;
                case BlendMode.Saturation: return SKBlendMode.Saturation;
                case BlendMode.Color: return SKBlendMode.Color;
                case BlendMode.Luminosity: return SKBlendMode.Luminosity;
            }
            throw new InvalidEnumArgumentException("Invalid BlendMode " + blendMode);
        }

        public RenderPaintStyle Style { set { SKPaint.Style = ToSKPaintStyle(value); } }
        public UInt32 Color { set { SKPaint.Color = value; } }
        public void LinearGradient(float sx, float sy,
                                   float ex, float ey,
                                   UInt32[] colors,
                                   float[] stops)
        {
            var skColors = new SKColor[colors.Length];
            for (int i = 0; i < colors.Length; ++i)
            {
                skColors[i] = colors[i];
            }
            SKPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(sx, sy),
                                                           new SKPoint(ex, ey),
                                                           skColors,
                                                           stops,
                                                           SKShaderTileMode.Clamp);
        }
        public void RadialGradient(float cx, float cy,
                                   float radius,
                                   UInt32[] colors,
                                   float[] stops)
        {
            var skColors = new SKColor[colors.Length];
            for (int i = 0; i < colors.Length; ++i)
            {
                skColors[i] = colors[i];
            }
            SKPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(cx, cy),
                                                           radius,
                                                           skColors,
                                                           stops,
                                                           SKShaderTileMode.Clamp);
        }
        public float Thickness { set { SKPaint.StrokeWidth = value; } }
        public StrokeJoin Join { set { SKPaint.StrokeJoin = ToSKStrokeJoin(value); } }
        public StrokeCap Cap { set { SKPaint.StrokeCap = ToSKStrokeCap(value); } }
        public BlendMode BlendMode { set { SKPaint.BlendMode = ToSKBlendMode(value); } }

        [MonoPInvokeCallback(typeof(RenderPaintDelegates.StyleDelegate))]
        static void StyleCallback(IntPtr @ref, int style)
        {
            var renderPaint = RiveAPI.CastNativeRef<RenderPaint>(@ref);
            renderPaint.Style = (RenderPaintStyle)style;
        }

        [MonoPInvokeCallback(typeof(RenderPaintDelegates.ColorDelegate))]
        static void ColorCallback(IntPtr @ref, UInt32 color)
        {
            var renderPaint = RiveAPI.CastNativeRef<RenderPaint>(@ref);
            renderPaint.Color = color;
        }

        [MonoPInvokeCallback(typeof(RenderPaintDelegates.LinearGradientDelegate))]
        static void LinearGradientCallback(IntPtr @ref,
                                           float sx, float sy,
                                           float ex, float ey,
                                           IntPtr colorsArray, IntPtr stopsArray, int n)
        {
            var renderPaint = RiveAPI.CastNativeRef<RenderPaint>(@ref);
            var colors = new UInt32[n];
            RiveAPI.CopyU32Array(colorsArray, colors, n);
            var stops = new float[n];
            Marshal.Copy(stopsArray, stops, 0, n);
            renderPaint.LinearGradient(sx, sy, ex, ey, colors, stops);
        }

        [MonoPInvokeCallback(typeof(RenderPaintDelegates.RadialGradientDelegate))]
        static void RadialGradientCallback(IntPtr @ref,
                                           float cx, float cy,
                                           float radius,
                                           IntPtr colorsArray, IntPtr stopsArray, int n)
        {
            var renderPaint = RiveAPI.CastNativeRef<RenderPaint>(@ref);
            var colors = new UInt32[n];
            RiveAPI.CopyU32Array(colorsArray, colors, n);
            var stops = new float[n];
            Marshal.Copy(stopsArray, stops, 0, n);
            renderPaint.RadialGradient(cx, cy, radius, colors, stops);
        }

        [MonoPInvokeCallback(typeof(RenderPaintDelegates.ThicknessDelegate))]
        static void ThicknessCallback(IntPtr @ref, float thickness)
        {
            var renderPaint = RiveAPI.CastNativeRef<RenderPaint>(@ref);
            renderPaint.Thickness = thickness;
        }

        [MonoPInvokeCallback(typeof(RenderPaintDelegates.JoinDelegate))]
        static void JoinCallback(IntPtr @ref, int join)
        {
            var renderPaint = RiveAPI.CastNativeRef<RenderPaint>(@ref);
            renderPaint.Join = (StrokeJoin)join;
        }

        [MonoPInvokeCallback(typeof(RenderPaintDelegates.CapDelegate))]
        static void CapCallback(IntPtr @ref, int cap)
        {
            var renderPaint = RiveAPI.CastNativeRef<RenderPaint>(@ref);
            renderPaint.Cap = (StrokeCap)cap;
        }

        [MonoPInvokeCallback(typeof(RenderPaintDelegates.BlendModeDelegate))]
        static void BlendModeCallback(IntPtr @ref, int blendMode)
        {
            var renderPaint = RiveAPI.CastNativeRef<RenderPaint>(@ref);
            renderPaint.BlendMode = (BlendMode)blendMode;
        }
    }
}
