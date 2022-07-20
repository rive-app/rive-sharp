using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RiveSharp
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct AABB
    {
        public float minX, minY, maxX, maxY;

        public AABB(float minX, float minY, float maxX, float maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Vec2D
    {
        public float x, y;

        public Vec2D(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Mat2D
    {
        public float x1, y1, x2, y2, tx, ty;

        public static Mat2D Identity { get { return new Mat2D(1, 0, 0, 1, 0, 0); } }

        public static Mat2D FromTranslate(float x, float y)
        {
            return new Mat2D(1, 0, 0, 1, x, y);
        }

        public static Mat2D FromScale(float sx, float sy)
        {
            return new Mat2D(sx, 0, 0, sy, 0, 0);
        }

        public static Mat2D FromRotation(float radians)
        {
            float sin = (float)Math.Sin(radians);
            float cos = (float)Math.Cos(radians);
            return new Mat2D(cos, sin, -sin, cos, 0, 0);
        }

        public Mat2D(float x1, float y1, float x2, float y2, float tx, float ty)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            this.tx = tx;
            this.ty = ty;
        }

        public static Mat2D operator *(Mat2D a, Mat2D b)
        {
            Mat2D product;
            Mat2D_Multiply(in a, in b, out product);
            return product;
        }

        public static Vec2D operator *(Mat2D a, Vec2D b)
        {
            Vec2D product;
            Mat2D_MultiplyVec2D(in a, in b, out product);
            return product;
        }

        public bool Invert(out Mat2D inverse)
        {
            return Mat2D_Invert(in this, out inverse);
        }

        public Mat2D InvertOrIdentity()
        {
            Mat2D inverse;
            return Invert(out inverse) ? inverse : Mat2D.Identity;
        }

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Mat2D_Multiply(in Mat2D a, in Mat2D b, out Mat2D c);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Mat2D_MultiplyVec2D(in Mat2D a, in Vec2D b, out Vec2D c);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Mat2D_Invert(in Mat2D a, out Mat2D b);
    }
}
