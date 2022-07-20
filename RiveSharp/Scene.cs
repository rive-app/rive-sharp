using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RiveSharp
{
    public enum Loop
    {
        // Play until the duration or end of work area of the animation.
        OneShot = 0,

        // Play until the duration or end of work area of the animation and then go back to the
        // start (0 seconds).
        Loop = 1,

        // Play to the end of the duration/work area and then play back.
        PingPong = 2
    };

    public class Scene
    {
        private IntPtr mNativePtr;

        public Scene() => mNativePtr = Scene_NativeNew(Factory.Instance.RefNative());
        ~Scene() => Scene_NativeDelete(mNativePtr);

        private bool mIsValid = false;
        public bool IsValid => mIsValid;

        public bool LoadFile(Stream stream)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            return LoadFile(data);
        }

        public bool LoadFile(byte[] data)
        {
            mIsValid = false;
            if (data == null || data.Length == 0)
            {
                return false;
            }
            return Scene_NativeLoadFile(mNativePtr, data, data.Length);
        }

        // Loads an artboard and animation from the already-loaded file.
        public bool LoadArtboard(string artboardName)
        {
            mIsValid = false;
            return Scene_NativeLoadArtboard(mNativePtr, artboardName);
        }

        // Loads a state machine from the already-loaded artboard and file.
        public bool LoadStateMachine(string stateMachineName)
        {
            return (mIsValid = Scene_NativeLoadStateMachine(mNativePtr, stateMachineName));
        }

        // Loads an animation from the already-loaded artboard and file.
        public bool LoadAnimation(string animationName)
        {
            return (mIsValid = Scene_NativeLoadAnimation(mNativePtr, animationName));
        }

        public void SetBool(string name, bool value)
        {
            if (this.IsValid && !Scene_NativeSetBool(mNativePtr, name, value))
                throw new Exception($"State machine bool input '{name}' not found.");
        }

        public void SetNumber(string name, float value)
        {
            if (this.IsValid && !Scene_NativeSetNumber(mNativePtr, name, value))
                throw new Exception($"State machine number input '{name}' not found.");
        }

        public void FireTrigger(string name)
        {
            if (this.IsValid && !Scene_NativeFireTrigger(mNativePtr, name))
                throw new Exception($"State machine trigger input '{name}' not found.");
        }

        public float Width => Scene_NativeWidth(mNativePtr);
        public float Height => Scene_NativeHeight(mNativePtr);
        public string Name => Scene_NativeName(mNativePtr);

        // Returns OneShot if this has no looping (e.g. a statemachine)
        public Loop Loop => (Loop)Scene_NativeLoop(mNativePtr);

        // Returns true iff the Scene is known to not be fully opaque
        public bool IsTranslucent => Scene_NativeIsTranslucent(mNativePtr);

        // returns -1 for continuous
        public double DurationSeconds => Scene_NativeDurationSeconds(mNativePtr);

        // returns true if Draw() should be called
        public bool AdvanceAndApply(double elapsedSeconds)
        {
            return Scene_NativeAdvanceAndApply(mNativePtr, (float)elapsedSeconds);
        }

        public void Draw(Renderer renderer)
        {
            var gch = GCHandle.Alloc(renderer);
            Scene_NativeDraw(mNativePtr, GCHandle.ToIntPtr(gch));
            gch.Free();
        }

        public void PointerDown(Vec2D pos) => Scene_NativePointerDown(mNativePtr, in pos);
        public void PointerMove(Vec2D pos) => Scene_NativePointerMove(mNativePtr, in pos);
        public void PointerUp(Vec2D pos) => Scene_NativePointerUp(mNativePtr, in pos);

        // Native interop.
        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Scene_NativeNew(IntPtr factoryPtr);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Scene_NativeDelete(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeLoadFile(IntPtr scene, byte[] fileBytes, int length);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeLoadArtboard(IntPtr scene, string name);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeLoadStateMachine(IntPtr scene, string name);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeLoadAnimation(IntPtr scene, string name);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeSetBool(IntPtr scene, string name, bool value);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeSetNumber(IntPtr scene, string name, float value);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeFireTrigger(IntPtr scene, string name);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern float Scene_NativeWidth(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern float Scene_NativeHeight(IntPtr scene);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern string Scene_NativeName(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Scene_NativeLoop(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeIsTranslucent(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern float Scene_NativeDurationSeconds(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Scene_NativeAdvanceAndApply(IntPtr scene, float elapsedSeconds);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Scene_NativeDraw(IntPtr scene, IntPtr renderer);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Scene_NativePointerDown(IntPtr scene, in Vec2D pos);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Scene_NativePointerMove(IntPtr scene, in Vec2D pos);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Scene_NativePointerUp(IntPtr scene, in Vec2D pos);
    }
}
