using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RiveSharp
{
    public class Factory
    {
        static Factory() => InitNativeDelegates();
        private static Factory sInstance = new Factory();
        public static Factory Instance => sInstance;
        private Factory() { }

        RenderPath MakeRenderPath() => new RenderPath();
        RenderPaint MakeRenderPaint() => new RenderPaint();
        RenderImage DecodeImage(byte[] data) => RenderImage.Decode(data);

        // Native interop.
        private delegate void NativeFreeDelegate(IntPtr ptr);
        private delegate IntPtr NativeMakeDelegate(IntPtr ptr);
        private delegate IntPtr NativeDecodeImageDelegate(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)]
            byte[] data,
            int n);

        internal IntPtr RefNative() => GCHandle.ToIntPtr(GCHandle.Alloc(this));
        internal static Factory FromNative(IntPtr ptr) => (Factory)GCHandle.FromIntPtr(ptr).Target;

        internal static void InitNativeDelegates()
        {
            Factory_NativeInitDelegates(
                (IntPtr ptr) => GCHandle.FromIntPtr(ptr).Free(),
                (IntPtr ptr) => GCHandle.ToIntPtr(GCHandle.Alloc(FromNative(ptr).MakeRenderPath())),
                (IntPtr ptr) => GCHandle.ToIntPtr(GCHandle.Alloc(FromNative(ptr).MakeRenderPaint())),
                (IntPtr ptr, byte[] data, int n) =>
                {
                    var image = FromNative(ptr).DecodeImage(data);
                    return (IntPtr)(image != null ? GCHandle.ToIntPtr(GCHandle.Alloc(image))
                                                  : IntPtr.Zero);
                });
        }

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Factory_NativeInitDelegates(NativeFreeDelegate free,
                                                               NativeMakeDelegate makeRenderPath,
                                                               NativeMakeDelegate makeRenderPaint,
                                                               NativeDecodeImageDelegate decodeImage);
    }
}
