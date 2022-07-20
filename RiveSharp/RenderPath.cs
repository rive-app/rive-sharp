using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

        private SKPath mSKPath = new SKPath();

        private static SKPathFillType ToSKPathFillType(FillRule rule)
        {
            switch (rule)
            {
                case FillRule.NonZero: return SKPathFillType.Winding;
                case FillRule.EvenOdd: return SKPathFillType.EvenOdd;
            }
            throw new InvalidEnumArgumentException("Invalid FillRule " + rule);
        }

        public SKPath SKPath { get { return mSKPath; } }
        public void Reset() { mSKPath.Reset(); }
        public void AddRenderPath(RenderPath path, in Mat2D m)
        {
            var mat = new SKMatrix(m.x1, m.x2, m.tx, m.y1, m.y2, m.ty, 0, 0, 1);
            mSKPath.AddPath(path.SKPath, ref mat);
        }
        public FillRule FillRule { set { mSKPath.FillType = ToSKPathFillType(value); } }
        public void MoveTo(float x, float y) { mSKPath.MoveTo(x, y); }
        public void LineTo(float x, float y) { mSKPath.LineTo(x, y); }
        public void CubicTo(float ox, float oy, float ix, float iy, float x, float y)
        {
            mSKPath.CubicTo(ox, oy, ix, iy, x, y);
        }
        public void Close() { mSKPath.Close(); }

        // Native interop.
        private delegate void NativeFreeDelegate(IntPtr ptr);
        private delegate void NativeResetDelegate(IntPtr ptr);
        private delegate void NativeAddRenderPathDelegate(IntPtr ptr, IntPtr path, in Mat2D m);
        private delegate void NativeFillRuleDelegate(IntPtr ptr, int rule);
        private delegate void NativeMoveToDelegate(IntPtr ptr, float x, float y);
        private delegate void NativeLineToDelegate(IntPtr ptr, float x, float y);
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
                                                                  NativeCubicToDelegate h,
                                                                  NativeCloseDelegate i);
    }
}
