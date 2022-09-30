// Copyright 2022 Rive

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;

namespace RiveSharp
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }
    }

    internal class RiveAPI
    {
        private const string Library = "rive";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopySKPointArray(IntPtr sourceArray, [Out] SKPoint[] destination, Int32 count);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopyU32Array(IntPtr sourceArray, [Out] UInt32[] destination, Int32 count);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CopyU16Array(IntPtr sourceArray, [Out] UInt16[] destination, Int32 count);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void Mat2D_Multiply(Mat2D a, Mat2D b, Mat2D* c);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void Mat2D_MultiplyVec2D(Mat2D a, Vec2D b, Vec2D* c);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe SByte Mat2D_Invert(Mat2D a, Mat2D* b);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Factory_RegisterDelegates(FactoryDelegates delegates);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Renderer_RegisterDelegates(RendererDelegates delegates);

        [StructLayout(LayoutKind.Sequential)]
        public struct ComputeAlignmentArgs
        {
            public Int32 Fit;
            public float AlignX;
            public float AlignY;
            public AABB Frame;
            public AABB Content;
            public Mat2D Matrix;
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern void Renderer_ComputeAlignment(ComputeAlignmentArgs* args);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RenderImage_RegisterDelegates(RenderImageDelegates delegates);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RenderPaint_RegisterDelegates(RenderPaintDelegates delegates);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RenderPath_RegisterDelegates(RenderPathDelegates delegates);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Scene_New(IntPtr factoryPtr);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Scene_Delete(IntPtr scene);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_LoadFile(IntPtr scene, [In] byte[] fileBytes, int length);

        [DllImport(Library, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_LoadArtboard(IntPtr scene, string name);

        [DllImport(Library, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_LoadStateMachine(IntPtr scene, string name);

        [DllImport(Library, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_LoadAnimation(IntPtr scene, string name);

        [DllImport(Library, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_SetBool(IntPtr scene, string name, Int32 value);

        [DllImport(Library, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_SetNumber(IntPtr scene, string name, float value);

        [DllImport(Library, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_FireTrigger(IntPtr scene, string name);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Single Scene_Width(IntPtr scene);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Single Scene_Height(IntPtr scene);

        [DllImport(Library, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 Scene_Name(IntPtr scene, [Out] char[] charArray);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 Scene_Loop(IntPtr scene);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_IsTranslucent(IntPtr scene);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Single Scene_DurationSeconds(IntPtr scene);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern SByte Scene_AdvanceAndApply(IntPtr scene, float elapsedSeconds);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Scene_Draw(IntPtr scene, IntPtr renderer);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Scene_PointerDown(IntPtr scene, Vec2D pos);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Scene_PointerMove(IntPtr scene, Vec2D pos);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Scene_PointerUp(IntPtr scene, Vec2D pos);

        public static IntPtr CreateNativeRef(Object obj)
        {
            return GCHandle.ToIntPtr(GCHandle.Alloc(obj));
        }

        public static T CastNativeRef<T>(IntPtr @ref)
        {
            return (T)GCHandle.FromIntPtr(@ref).Target;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void ReleaseNativeRefDelegate(IntPtr ptr);

        [MonoPInvokeCallback(typeof(ReleaseNativeRefDelegate))]
        public static void ReleaseNativeRefCallback(IntPtr @ref)
        {
            GCHandle.FromIntPtr(@ref).Free();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FactoryDelegates
    {
        public RiveAPI.ReleaseNativeRefDelegate Release;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate IntPtr MakeRenderPathDelegate(IntPtr @ref,
                                                             IntPtr ptsArray,  // SKPoint[nPts]
                                                             Int32 nPts,
                                                             IntPtr verbsArray,  // byte[nVerbs]
                                                             Int32 nVerbs,
                                                             Int32 fillRule);
        public MakeRenderPathDelegate MakeRenderPath;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate IntPtr MakeEmptyObjectDelegate(IntPtr @ref);
        public MakeEmptyObjectDelegate MakeEmptyRenderPath;
        public MakeEmptyObjectDelegate MakeRenderPaint;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate IntPtr DecodeImageDelegate(IntPtr @ref, IntPtr bytesArray, Int32 nBytes);
        public DecodeImageDelegate DecodeImage;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct RendererDelegates
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void SaveDelegate(IntPtr @ref);
        public SaveDelegate Save;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void RestoreDelegate(IntPtr @ref);
        public RestoreDelegate Restore;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void TransformDelegate(IntPtr @ref,
                                                      float x1, float y1,
                                                      float x2, float y2,
                                                      float tx, float ty);
        public TransformDelegate Transform;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void DrawPathDelegate(IntPtr @ref, IntPtr pathRef, IntPtr paintRef);
        public DrawPathDelegate DrawPath;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void ClipPathDelegate(IntPtr @ref, IntPtr pathPtr);
        public ClipPathDelegate ClipPath;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void DrawImageDelegate(IntPtr @ref,
                                                      IntPtr imageRef,
                                                      Int32 blendMode,
                                                      float opacity);
        public DrawImageDelegate DrawImage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void DrawImageMeshDelegate(IntPtr @ref,
                                                          IntPtr imageRef,
                                                          IntPtr vertexArray,  // SKPoint[nVertices]
                                                          IntPtr uvArray,  // SKPoint[nVertices]
                                                          Int32 nVertices,
                                                          IntPtr indexArray,  // UInt16[nIndices]
                                                          Int32 nIndices,
                                                          Int32 blendMode,
                                                          float opacity);
        public DrawImageMeshDelegate DrawImageMesh;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct RenderImageDelegates
    {
        public RiveAPI.ReleaseNativeRefDelegate Release;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate Int32 WidthHeightDelegate(IntPtr @ref);
        public WidthHeightDelegate Width;
        public WidthHeightDelegate Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct RenderPaintDelegates
    {
        public RiveAPI.ReleaseNativeRefDelegate Release;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void StyleDelegate(IntPtr @ref, Int32 style);
        public StyleDelegate Style;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void ColorDelegate(IntPtr @ref, UInt32 color);
        public ColorDelegate Color;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void LinearGradientDelegate(IntPtr @ref,
                                                           float sx, float sy,
                                                           float ex, float ey,
                                                           IntPtr colorsArray,  // UInt32[n]
                                                           IntPtr stopsArray,  // float[n]
                                                           Int32 n);
        public LinearGradientDelegate LinearGradient;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void RadialGradientDelegate(IntPtr @ref,
                                                           float cx, float cy,
                                                           float radius,
                                                           IntPtr colorsArray,  // UInt32[n]
                                                           IntPtr stopsArray,  // float[n]
                                                           Int32 n);
        public RadialGradientDelegate RadialGradient;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void ThicknessDelegate(IntPtr @ref, float thickness);
        public ThicknessDelegate Thickness;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void JoinDelegate(IntPtr @ref, Int32 join);
        public JoinDelegate Join;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void CapDelegate(IntPtr @ref, Int32 cap);
        public CapDelegate Cap;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void BlendModeDelegate(IntPtr @ref, Int32 blendMode);
        public BlendModeDelegate BlendMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct RenderPathDelegates
    {
        public RiveAPI.ReleaseNativeRefDelegate Release;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void ResetDelegate(IntPtr ptr);
        public ResetDelegate Reset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void AddRenderPathDelegate(IntPtr ptr,
                                                          IntPtr path,
                                                          float x1, float y1,
                                                          float x2, float y2,
                                                          float tx, float ty);
        public AddRenderPathDelegate AddRenderPath;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void FillRuleDelegate(IntPtr ptr, int rule);
        public FillRuleDelegate FillRule;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void MoveToDelegate(IntPtr ptr, float x, float y);
        public MoveToDelegate MoveTo;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void LineToDelegate(IntPtr ptr, float x, float y);
        public LineToDelegate LineTo;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void QuadToDelegate(IntPtr ptr,
                                                   float os, float oy,
                                                   float x, float y);
        public QuadToDelegate QuadTo;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void CubicToDelegate(IntPtr ptr,
                                                    float ox, float oy,
                                                    float ix, float iy,
                                                    float x, float y);
        public CubicToDelegate CubicTo;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void CloseDelegate(IntPtr ptr);
        public CloseDelegate Close;
    }
}
