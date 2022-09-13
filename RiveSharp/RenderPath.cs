// Copyright 2022 Rive

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace RiveSharp
{
    public enum FillRule
    {
        NonZero = 0,
        EvenOdd = 1
    }

    public class RenderPath
    {
        static RenderPath() => InitNativeDelegates();

        public readonly SKPath SKPath = new SKPath();

        private static SKPathFillType ToSKPathFillType(FillRule rule)
        {
            switch (rule)
            {
                case FillRule.NonZero: return SKPathFillType.Winding;
                case FillRule.EvenOdd: return SKPathFillType.EvenOdd;
            }
            throw new InvalidEnumArgumentException("Invalid FillRule " + rule);
        }

        public RenderPath(SKPoint[] pts, byte[] verbs, FillRule fillRule)
        {
            SKPath.FillType = ToSKPathFillType(fillRule);

            // Unfortuately, SkiaSharp doesn't appear to expose the SkPathBuilder API yet.
            int ptsIdx = 0;
            foreach (byte v in verbs)
            {
                switch ((SKPathVerb)v)
                {
                    case SKPathVerb.Move:
                        SKPath.MoveTo(pts[ptsIdx]);
                        ptsIdx++;
                        break;
                    case SKPathVerb.Line:
                        SKPath.LineTo(pts[ptsIdx]);
                        ptsIdx++;
                        break;
                    case SKPathVerb.Quad:
                        SKPath.QuadTo(pts[ptsIdx], pts[ptsIdx + 1]);
                        ptsIdx += 2;
                        break;
                    case SKPathVerb.Cubic:
                        SKPath.CubicTo(pts[ptsIdx], pts[ptsIdx + 1], pts[ptsIdx + 2]);
                        ptsIdx += 3;
                        break;
                    case SKPathVerb.Close:
                        SKPath.Close();
                        break;
                    default:
                        throw new Exception("invalid path verb");
                }
            }
            if (ptsIdx != pts.Length)
            {
                throw new Exception("invalid number of points");
            }
        }

        public RenderPath() { }

        public void Reset() { SKPath.Reset(); }
        public void AddRenderPath(RenderPath path, in Mat2D m)
        {
            var mat = new SKMatrix(m.X1, m.X2, m.Tx, m.Y1, m.Y2, m.Ty, 0, 0, 1);
            SKPath.AddPath(path.SKPath, ref mat);
        }
        public FillRule FillRule { set { SKPath.FillType = ToSKPathFillType(value); } }
        public void MoveTo(float x, float y) { SKPath.MoveTo(x, y); }
        public void LineTo(float x, float y) { SKPath.LineTo(x, y); }
        public void QuadTo(float ox, float oy, float x, float y)
        {
            SKPath.QuadTo(ox, oy, x, y);
        }
        public void CubicTo(float ox, float oy, float ix, float iy, float x, float y)
        {
            SKPath.CubicTo(ox, oy, ix, iy, x, y);
        }
        public void Close() { SKPath.Close(); }

        // Native interop.
        private delegate void NativeFreeDelegate(IntPtr ptr);
        private delegate void NativeResetDelegate(IntPtr ptr);
        private delegate void NativeAddRenderPathDelegate(IntPtr ptr, IntPtr path, in Mat2D m);
        private delegate void NativeFillRuleDelegate(IntPtr ptr, int rule);
        private delegate void NativeMoveToDelegate(IntPtr ptr, float x, float y);
        private delegate void NativeLineToDelegate(IntPtr ptr, float x, float y);
        private delegate void NativeQuadToDelegate(IntPtr ptr,
                                                   float os, float oy,
                                                   float x, float y);
        private delegate void NativeCubicToDelegate(IntPtr ptr,
                                                    float ox, float oy,
                                                    float ix, float iy,
                                                    float x, float y);
        private delegate void NativeCloseDelegate(IntPtr ptr);

        internal static RenderPath FromNative(IntPtr ptr)
        {
            return (RenderPath)GCHandle.FromIntPtr(ptr).Target;
        }

        internal static void InitNativeDelegates()
        {
            RenderPath_NativeInitDelegates(
                (IntPtr ptr) => GCHandle.FromIntPtr(ptr).Free(),
                (IntPtr ptr) => FromNative(ptr).Reset(),
                (IntPtr ptr, IntPtr path, in Mat2D m)
                    => FromNative(ptr).AddRenderPath(FromNative(path), m),
                (IntPtr ptr, int rule) => FromNative(ptr).FillRule = (FillRule)rule,
                (IntPtr ptr, float x, float y) => FromNative(ptr).MoveTo(x, y),
                (IntPtr ptr, float x, float y) => FromNative(ptr).LineTo(x, y),
                (IntPtr ptr, float ox, float oy, float x, float y)
                    => FromNative(ptr).QuadTo(ox, oy, x, y),
                (IntPtr ptr, float ox, float oy, float ix, float iy, float x, float y)
                    => FromNative(ptr).CubicTo(ox, oy, ix, iy, x, y),
                (IntPtr ptr) => FromNative(ptr).Close());
        }

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void RenderPath_NativeInitDelegates(NativeFreeDelegate b,
                                                                  NativeResetDelegate c,
                                                                  NativeAddRenderPathDelegate d,
                                                                  NativeFillRuleDelegate e,
                                                                  NativeMoveToDelegate f,
                                                                  NativeLineToDelegate g,
                                                                  NativeQuadToDelegate h,
                                                                  NativeCubicToDelegate i,
                                                                  NativeCloseDelegate j);
    }
}
