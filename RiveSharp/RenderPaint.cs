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
        static RenderPaint() => InitNativeDelegates();

        private SKPaint mSKPaint = new SKPaint
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

        public SKPaint SKPaint { get { return mSKPaint; } }
        public RenderPaintStyle Style { set { mSKPaint.Style = ToSKPaintStyle(value); } }
        public UInt32 Color { set { mSKPaint.Color = value; } }
        public void LinearGradient(float sx, float sy,
                                   float ex, float ey,
                                   UInt32[] colors,
                                   float[] stops)
        {
            SkiaSharp.SKColor[] skColors = new SKColor[colors.Length];
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
            SkiaSharp.SKColor[] skColors = new SKColor[colors.Length];
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
        public float Thickness { set { mSKPaint.StrokeWidth = value; } }
        public StrokeJoin Join { set { mSKPaint.StrokeJoin = ToSKStrokeJoin(value); } }
        public StrokeCap Cap { set { mSKPaint.StrokeCap = ToSKStrokeCap(value); } }
        public BlendMode BlendMode { set { mSKPaint.BlendMode = ToSKBlendMode(value); } }

        // Native interop.
        private delegate void NativeFreeDelegate(IntPtr ptr);
        private delegate void NativeStyleDelegate(IntPtr ptr, int style);
        private delegate void NativeColorDelegate(IntPtr ptr, UInt32 color);
        private delegate void NativeLinearGradientDelegate(
                IntPtr ptr,
                float sx, float sy,
                float ex, float ey,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=7)]
                UInt32[] colors,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=7)]
                float[] stops,
                int count);
        private delegate void NativeRadialGradientDelegate(
                IntPtr ptr,
                float cx, float cy,
                float radius,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=6)]
                UInt32[] colors,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=6)]
                float[] stops,
                int count);
        private delegate void NativeThicknessDelegate(IntPtr ptr, float thickness);
        private delegate void NativeJoinDelegate(IntPtr ptr, int join);
        private delegate void NativeCapDelegate(IntPtr ptr, int cap);
        private delegate void NativeBlendModeDelegate(IntPtr ptr, int blendMode);

        internal static RenderPaint FromNative(IntPtr ptr)
        {
            return (RenderPaint)GCHandle.FromIntPtr(ptr).Target;
        }

        internal static void InitNativeDelegates()
        {
            RenderPaint_NativeInitDelegates(
                (IntPtr ptr) => GCHandle.FromIntPtr(ptr).Free(),
                (IntPtr ptr, int style) => FromNative(ptr).Style = (RenderPaintStyle)style,
                (IntPtr ptr, UInt32 color) => FromNative(ptr).Color = color,
                (IntPtr ptr, float sx, float sy, float ex, float ey, UInt32[] c, float[] s, int n)
                    => FromNative(ptr).LinearGradient(sx, sy, ex, ey, c, s),
                (IntPtr ptr, float cx, float cy, float radius, UInt32[] c, float[] s, int n)
                    => FromNative(ptr).RadialGradient(cx, cy, radius, c, s),
                (IntPtr ptr, float thickness) => FromNative(ptr).Thickness = thickness,
                (IntPtr ptr, int join) => FromNative(ptr).Join = (StrokeJoin)join,
                (IntPtr ptr, int cap) => FromNative(ptr).Cap = (StrokeCap)cap,
                (IntPtr ptr, int blendMode) => FromNative(ptr).BlendMode = (BlendMode)blendMode);
        }

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void RenderPaint_NativeInitDelegates(NativeFreeDelegate b,
                                                                   NativeStyleDelegate c,
                                                                   NativeColorDelegate d,
                                                                   NativeLinearGradientDelegate e,
                                                                   NativeRadialGradientDelegate f,
                                                                   NativeThicknessDelegate g,
                                                                   NativeJoinDelegate h,
                                                                   NativeCapDelegate i,
                                                                   NativeBlendModeDelegate j);
    }
}
