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
        static readonly RenderPathDelegates Delegates = new RenderPathDelegates
        {
            Release = RiveAPI.ReleaseNativeRefCallback,
            Reset = ResetCallback,
            AddRenderPath = AddRenderPathCallback,
            FillRule = FillRuleCallback,
            MoveTo = MoveToCallback,
            LineTo = LineToCallback,
            QuadTo = QuadToCallback,
            CubicTo = CubicToCallback,
            Close = CloseCallback
        };

        static RenderPath()
        {
            RiveAPI.RenderPath_RegisterDelegates(Delegates);
        }

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
        public void AddRenderPath(RenderPath path, Mat2D m)
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

        [MonoPInvokeCallback(typeof(RenderPathDelegates.ResetDelegate))]
        static void ResetCallback(IntPtr @ref)
        {
            var renderPath = RiveAPI.CastNativeRef<RenderPath>(@ref);
            renderPath.Reset();
        }

        [MonoPInvokeCallback(typeof(RenderPathDelegates.AddRenderPathDelegate))]
        static void AddRenderPathCallback(IntPtr @ref,
                                          IntPtr pathRef,
                                          float x1, float y1,
                                          float x2, float y2,
                                          float tx, float ty)
        {
            var renderPath = RiveAPI.CastNativeRef<RenderPath>(@ref);
            var path = RiveAPI.CastNativeRef<RenderPath>(pathRef);
            renderPath.AddRenderPath(path, new Mat2D(x1, y1, x2, y2, tx, ty));
        }

        [MonoPInvokeCallback(typeof(RenderPathDelegates.FillRuleDelegate))]
        static void FillRuleCallback(IntPtr @ref, int rule)
        {
            var renderPath = RiveAPI.CastNativeRef<RenderPath>(@ref);
            renderPath.FillRule = (FillRule)rule;
        }

        [MonoPInvokeCallback(typeof(RenderPathDelegates.MoveToDelegate))]
        static void MoveToCallback(IntPtr @ref, float x, float y)
        {
            var renderPath = RiveAPI.CastNativeRef<RenderPath>(@ref);
            renderPath.MoveTo(x, y);
        }

        [MonoPInvokeCallback(typeof(RenderPathDelegates.LineToDelegate))]
        static void LineToCallback(IntPtr @ref, float x, float y)
        {
            var renderPath = RiveAPI.CastNativeRef<RenderPath>(@ref);
            renderPath.LineTo(x, y);
        }

        [MonoPInvokeCallback(typeof(RenderPathDelegates.QuadToDelegate))]
        static void QuadToCallback(IntPtr @ref, float ox, float oy, float x, float y)
        {
            var renderPath = RiveAPI.CastNativeRef<RenderPath>(@ref);
            renderPath.QuadTo(ox, oy, x, y);
        }

        [MonoPInvokeCallback(typeof(RenderPathDelegates.CubicToDelegate))]
        static void CubicToCallback(IntPtr @ref, float ox, float oy, float ix, float iy, float x, float y)
        {
            var renderPath = RiveAPI.CastNativeRef<RenderPath>(@ref);
            renderPath.CubicTo(ox, oy, ix, iy, x, y);
        }

        [MonoPInvokeCallback(typeof(RenderPathDelegates.CloseDelegate))]
        static void CloseCallback(IntPtr @ref)
        {
            var renderPath = RiveAPI.CastNativeRef<RenderPath>(@ref);
            renderPath.Close();
        }
    }
}
