// Copyright 2022 Rive

using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace RiveSharp
{
    public enum Fit
    {
        Fill = 0,
        Contain = 1,
        Cover = 2,
        FitWidth = 3,
        FitHeight = 4,
        None = 5,
        ScaleDown = 6
    };

    public class Alignment
    {
        public readonly float X, Y;

        Alignment(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Alignment TopLeft { get { return new Alignment(1.0f, -1.0f); } }
        public static Alignment TopCenter { get { return new Alignment(0.0f, -1.0f); } }
        public static Alignment TopRight { get { return new Alignment(1.0f, -1.0f); } }
        public static Alignment CenterLeft { get { return new Alignment(1.0f, 0.0f); } }
        public static Alignment Center { get { return new Alignment(0.0f, 0.0f); } }
        public static Alignment CenterRight { get { return new Alignment(1.0f, 0.0f); } }
        public static Alignment BottomLeft { get { return new Alignment(1.0f, 1.0f); } }
        public static Alignment BottomCenter { get { return new Alignment(0.0f, 1.0f); } }
        public static Alignment BottomRight { get { return new Alignment(1.0f, 1.0f); } }
    }


    public class Renderer
    {
        static Renderer() => InitNativeDelegates();

        public readonly SKCanvas SKCanvas;

        public Renderer(SKCanvas skCanvas)
        {
            SKCanvas = skCanvas;
        }

        public void Save() { SKCanvas.Save(); }
        public void Restore() { SKCanvas.Restore(); }

        public void Transform(Mat2D m)
        {
            var mat = new SKMatrix(m.X1, m.X2, m.Tx, m.Y1, m.Y2, m.Ty, 0, 0, 1);
            SKCanvas.Concat(ref mat);
        }

        public void DrawPath(RenderPath path, RenderPaint paint)
        {
            SKCanvas.DrawPath(path.SKPath, paint.SKPaint);
        }

        public void ClipPath(RenderPath path)
        {
            SKCanvas.ClipPath(path.SKPath, SKClipOperation.Intersect, true);
        }

        public void DrawImage(RenderImage image, BlendMode blendMode, float opacity)
        {
            SKCanvas.DrawImage(image.SKImage, 0, 0, new SKPaint
            {
                IsAntialias = true,
                ColorF = new SKColorF(1, 1, 1, opacity),
                BlendMode = RenderPaint.ToSKBlendMode(blendMode),
            });
        }

        public void DrawImageMesh(RenderImage image,
                                  SKPoint[] vertices,
                                  SKPoint[] uvs,
                                  UInt16[] indices,
                                  BlendMode blendMode,
                                  float opacity) {
            if (uvs.Length != vertices.Length)
            {
                throw new ArgumentException("uvs must be the same length as vertices.");
            }
            var skVertices = SKVertices.CreateCopy(SKVertexMode.Triangles,
                                                   positions: vertices,
                                                   texs: uvs,
                                                   colors: null,
                                                   indices: indices);
            // DrawVertices ignores the blend mode if we don't have colors && uvs.
            SKCanvas.DrawVertices(skVertices, SKBlendMode.Dst, new SKPaint
            {
                IsAntialias = false,  // DrawVertices ignores the IsAntialias flag.
                ColorF = new SKColorF(1, 1, 1, opacity),
                Shader = image.SKImage.ToShader(),
                BlendMode = RenderPaint.ToSKBlendMode(blendMode),
            });
        }

        // Transformation helpers.
        public void Translate(float x, float y) { Transform(Mat2D.FromTranslate(x, y)); }
        public void Scale(float sx, float sy) { Transform(Mat2D.FromScale(sx, sy)); }
        public void Rotate(float radians) { Transform(Mat2D.FromRotation(radians));  }
        public void Align(Fit fit, Alignment alignment, AABB frame, AABB content)
        {
            Transform(ComputeAlignment(fit, alignment, frame, content));
        }

        public static Mat2D ComputeAlignment(Fit fit,
                                             Alignment alignment,
                                             AABB frame,
                                             AABB content)
        {
            Renderer_NativeComputeAlignment((int)fit, alignment.X, alignment.Y,
                                            in frame, in content, out Mat2D m);
            return m;
        }

        // Native interop.
        private delegate void NativeSaveDelegate(IntPtr ptr);
        private delegate void NativeRestoreDelegate(IntPtr ptr);
        private delegate void NativeTransformDelegate(IntPtr ptr, in Mat2D mat);
        private delegate void NativeDrawPathDelegate(IntPtr ptr, IntPtr pathPtr, IntPtr paintPtr);
        private delegate void NativeClipPathDelegate(IntPtr ptr, IntPtr pathPtr);
        private delegate void NativeDrawImageDelegate(IntPtr ptr,
                                                      IntPtr imagePtr,
                                                      int blendMode,
                                                      float opacity);
        private delegate void NativeDrawImageMeshDelegate(
                IntPtr ptr,
                IntPtr imagePtr,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=4)]
                SKPoint[] vertices,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=4)]
                SKPoint[] uvs,
                int vertexFloatCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=6)]
                UInt16[] indices,
                int indexCount,
                int blendMode,
                float opacity);

        private static Renderer FromNative(IntPtr ptr)
        {
            return (Renderer)GCHandle.FromIntPtr(ptr).Target;
        }

        internal static void InitNativeDelegates()
        {
            Renderer_NativeInitDelegates(
                (IntPtr ptr) => FromNative(ptr).Save(),
                (IntPtr ptr) => FromNative(ptr).Restore(),
                (IntPtr ptr, in Mat2D mat) => FromNative(ptr).Transform(mat),
                (IntPtr ptr, IntPtr pathPtr, IntPtr paintPtr)
                    => FromNative(ptr).DrawPath(RenderPath.FromNative(pathPtr),
                                                RenderPaint.FromNative(paintPtr)),
                (IntPtr ptr, IntPtr pathPtr)
                    => FromNative(ptr).ClipPath(RenderPath.FromNative(pathPtr)),
                (IntPtr ptr, IntPtr imagePtr, int blendMode, float opacity)
                    => FromNative(ptr).DrawImage(RenderImage.FromNative(imagePtr),
                                                 (BlendMode)blendMode, opacity),
                (IntPtr ptr, IntPtr imagePtr, SKPoint[] vertices, SKPoint[] uvs,
                 int vertexCount, UInt16[] indices, int indexCount, int blendMode, float opacity)
                    => FromNative(ptr).DrawImageMesh(RenderImage.FromNative(imagePtr),
                                                     vertices, uvs, indices,
                                                     (BlendMode)blendMode, opacity));
        }

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Renderer_NativeInitDelegates(NativeSaveDelegate a,
                                                                NativeRestoreDelegate b,
                                                                NativeTransformDelegate c,
                                                                NativeDrawPathDelegate d,
                                                                NativeClipPathDelegate e,
                                                                NativeDrawImageDelegate f,
                                                                NativeDrawImageMeshDelegate g);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Renderer_NativeComputeAlignment(int fit,
                                                                   float alignX, float alignY,
                                                                   in AABB frame,
                                                                   in AABB content,
                                                                   out Mat2D outMatrix);

    }
}
