// Copyright 2022 Rive

using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using static RiveSharp.RiveAPI;

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
        static readonly RendererDelegates Delegates = new RendererDelegates
        {
            Save = SaveCallback,
            Restore = RestoreCallback,
            Transform = TransformCallback,
            DrawPath = DrawPathCallback,
            ClipPath = ClipPathCallback,
            DrawImage = DrawImageCallback,
            DrawImageMesh = DrawImageMeshCallback
        };

        static Renderer()
        {
            RiveAPI.Renderer_RegisterDelegates(Delegates);
        }

        public static unsafe Mat2D ComputeAlignment(Fit fit,
                                                    Alignment alignment,
                                                    AABB frame,
                                                    AABB content)
        {
            var args = new RiveAPI.ComputeAlignmentArgs
            {
                Fit = (int)fit,
                AlignX = alignment.X,
                AlignY = alignment.Y,
                Frame = frame,
                Content = content
            };
            RiveAPI.Renderer_ComputeAlignment(&args);
            return args.Matrix;
        }

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

        [MonoPInvokeCallback(typeof(RendererDelegates.SaveDelegate))]
        static void SaveCallback(IntPtr @ref)
        {
            var renderer = RiveAPI.CastNativeRef<Renderer>(@ref);
            renderer.Save();
        }

        [MonoPInvokeCallback(typeof(RendererDelegates.RestoreDelegate))]
        static void RestoreCallback(IntPtr @ref)
        {
            var renderer = RiveAPI.CastNativeRef<Renderer>(@ref);
            renderer.Restore();
        }

        [MonoPInvokeCallback(typeof(RendererDelegates.TransformDelegate))]
        static void TransformCallback(IntPtr @ref,
                                      float x1, float y1,
                                      float x2, float y2,
                                      float tx, float ty)
        {
            var renderer = RiveAPI.CastNativeRef<Renderer>(@ref);
            renderer.Transform(new Mat2D(x1, y1, x2, y2, tx, ty));
        }

        [MonoPInvokeCallback(typeof(RendererDelegates.DrawPathDelegate))]
        static void DrawPathCallback(IntPtr @ref, IntPtr pathRef, IntPtr paintRef)
        {
            var renderer = RiveAPI.CastNativeRef<Renderer>(@ref);
            var path = RiveAPI.CastNativeRef<RenderPath>(pathRef);
            var paint = RiveAPI.CastNativeRef<RenderPaint>(paintRef);
            renderer.DrawPath(path, paint);
        }

        [MonoPInvokeCallback(typeof(RendererDelegates.ClipPathDelegate))]
        static void ClipPathCallback(IntPtr @ref, IntPtr pathRef)
        {
            var renderer = RiveAPI.CastNativeRef<Renderer>(@ref);
            var path = RiveAPI.CastNativeRef<RenderPath>(pathRef);
            renderer.ClipPath(path);
        }

        [MonoPInvokeCallback(typeof(RendererDelegates.DrawImageDelegate))]
        static void DrawImageCallback(IntPtr @ref, IntPtr imageRef, int blendMode, float opacity)
        {
            var renderer = RiveAPI.CastNativeRef<Renderer>(@ref);
            var image = RiveAPI.CastNativeRef<RenderImage>(imageRef);
            renderer.DrawImage(image, (BlendMode)blendMode, opacity);
        }

        [MonoPInvokeCallback(typeof(RendererDelegates.DrawImageMeshDelegate))]
        static void DrawImageMeshCallback(IntPtr @ref,
                                          IntPtr imageRef,
                                          IntPtr vertexArray,  // SKPoint[nVertices]
                                          IntPtr uvArray,  // SKPoint[nVertices]
                                          Int32 nVertices,
                                          IntPtr indexArray,  // UInt16[nIndices]
                                          Int32 nIndices,
                                          Int32 blendMode,
                                          float opacity)
        {
            var renderer = RiveAPI.CastNativeRef<Renderer>(@ref);
            var image = RiveAPI.CastNativeRef<RenderImage>(imageRef);
            var vertices = new SKPoint[nVertices];
            RiveAPI.CopySKPointArray(vertexArray, vertices, nVertices);
            var uvs = new SKPoint[nVertices];
            RiveAPI.CopySKPointArray(uvArray, uvs, nVertices);
            var indices = new UInt16[nIndices];
            RiveAPI.CopyU16Array(indexArray, indices, nIndices);
            renderer.DrawImageMesh(image, vertices, uvs, indices, (BlendMode)blendMode, opacity);
        }
    }
}
