// Copyright 2022 Rive

using System;
using System.Runtime.InteropServices;

namespace RiveSharp
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AABB
    {
        public float MinX, MinY, MaxX, MaxY;

        public AABB(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vec2D
    {
        public float X, Y;

        public Vec2D(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Mat2D
    {
        public float X1, Y1, X2, Y2, Tx, Ty;

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
            this.X1 = x1;
            this.Y1 = y1;
            this.X2 = x2;
            this.Y2 = y2;
            this.Tx = tx;
            this.Ty = ty;
        }

        public static unsafe Mat2D operator *(Mat2D a, Mat2D b)
        {
            var product = new Mat2D();
            RiveAPI.Mat2D_Multiply(a, b, &product);
            return product;
        }

        public static unsafe Vec2D operator *(Mat2D a, Vec2D b)
        {
            var product = new Vec2D();
            RiveAPI.Mat2D_MultiplyVec2D(a, b, &product);
            return product;
        }

        public unsafe bool Invert(out Mat2D inverse)
        {
            var result = new Mat2D();
            if (RiveAPI.Mat2D_Invert(this, &result) != 0)
            {
                inverse = result;
                return true;
            }
            else
            {
                inverse = Mat2D.Identity;
                return false;
            }
        }

        public Mat2D InvertOrIdentity()
        {
            return Invert(out Mat2D inverse) ? inverse : Mat2D.Identity;
        }
    }
}
