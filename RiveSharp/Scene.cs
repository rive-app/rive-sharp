// Copyright 2022 Rive

using System;
using System.IO;
using System.Runtime.InteropServices;

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
        public readonly IntPtr NativePtr;

        public Scene()
        {
            NativePtr = Scene_NativeNew(Factory.Instance.RefNative());
        }
        ~Scene()
        {
            Scene_NativeDelete(NativePtr);
        }

        private bool _isLoaded = false;
        public bool IsLoaded => _isLoaded;

        public bool LoadFile(Stream stream)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            return LoadFile(data);
        }

        public bool LoadFile(byte[] data)
        {
            _isLoaded = false;
            if (data == null || data.Length == 0)
            {
                return false;
            }
            return Scene_NativeLoadFile(NativePtr, data, data.Length) != 0;
        }

        // Loads an artboard and animation from the already-loaded file.
        public bool LoadArtboard(string artboardName)
        {
            _isLoaded = false;
            return Scene_NativeLoadArtboard(NativePtr, artboardName) != 0;
        }

        // Loads a state machine from the already-loaded artboard and file.
        public bool LoadStateMachine(string stateMachineName)
        {
            _isLoaded = Scene_NativeLoadStateMachine(NativePtr, stateMachineName) != 0;
            return this.IsLoaded;
        }

        // Loads an animation from the already-loaded artboard and file.
        public bool LoadAnimation(string animationName)
        {
            _isLoaded = Scene_NativeLoadAnimation(NativePtr, animationName) != 0;
            return this.IsLoaded;
        }

        public void SetBool(string name, bool value)
        {
            if (this.IsLoaded && Scene_NativeSetBool(NativePtr, name, value) == 0)
            {
                throw new Exception($"State machine bool input '{name}' not found.");
            }
        }

        public void SetNumber(string name, float value)
        {
            if (this.IsLoaded && Scene_NativeSetNumber(NativePtr, name, value) == 0)
            {
                throw new Exception($"State machine number input '{name}' not found.");
            }
        }

        public void FireTrigger(string name)
        {
            if (this.IsLoaded && Scene_NativeFireTrigger(NativePtr, name) == 0)
            {
                throw new Exception($"State machine trigger input '{name}' not found.");
            }
        }

        public float Width => Scene_NativeWidth(NativePtr);
        public float Height => Scene_NativeHeight(NativePtr);
        public string Name => Scene_NativeName(NativePtr);

        // Returns OneShot if this has no looping (e.g. a statemachine)
        public Loop Loop => (Loop)Scene_NativeLoop(NativePtr);

        // Returns true iff the Scene is known to not be fully opaque
        public bool IsTranslucent => Scene_NativeIsTranslucent(NativePtr) != 0;

        // returns -1 for continuous
        public double DurationSeconds => Scene_NativeDurationSeconds(NativePtr);

        // returns true if Draw() should be called
        public bool AdvanceAndApply(double elapsedSeconds)
        {
            return Scene_NativeAdvanceAndApply(NativePtr, (float)elapsedSeconds) != 0;
        }

        public void Draw(Renderer renderer)
        {
            var gch = GCHandle.Alloc(renderer);
            Scene_NativeDraw(NativePtr, GCHandle.ToIntPtr(gch));
            gch.Free();
        }

        public void PointerDown(Vec2D pos) => Scene_NativePointerDown(NativePtr, in pos);
        public void PointerMove(Vec2D pos) => Scene_NativePointerMove(NativePtr, in pos);
        public void PointerUp(Vec2D pos) => Scene_NativePointerUp(NativePtr, in pos);

        // Native interop.
        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Scene_NativeNew(IntPtr factoryPtr);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Scene_NativeDelete(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeLoadFile(IntPtr scene, byte[] fileBytes, int length);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeLoadArtboard(IntPtr scene, string name);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeLoadStateMachine(IntPtr scene, string name);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeLoadAnimation(IntPtr scene, string name);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeSetBool(IntPtr scene, string name, bool value);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeSetNumber(IntPtr scene, string name, float value);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeFireTrigger(IntPtr scene, string name);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Single Scene_NativeWidth(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Single Scene_NativeHeight(IntPtr scene);

        [DllImport("rive.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern String Scene_NativeName(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 Scene_NativeLoop(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeIsTranslucent(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Single Scene_NativeDurationSeconds(IntPtr scene);

        [DllImport("rive.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte Scene_NativeAdvanceAndApply(IntPtr scene, float elapsedSeconds);

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
