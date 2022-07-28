using SkiaSharp.Views.UWP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace RiveSharp.Views
{
    // Implements a simple view that renders content from a .riv file.
    public class Panel : SKSwapChainPanel
    {
        public Panel()
        {
            this.Loaded += OnLoaded;
            this.PointerPressed +=
                (object s, PointerRoutedEventArgs e) => HandlePointerEvent(mScene.PointerDown, e);
            this.PointerMoved +=
                (object s, PointerRoutedEventArgs e) => HandlePointerEvent(mScene.PointerMove, e);
            this.PointerReleased +=
                (object s, PointerRoutedEventArgs e) => HandlePointerEvent(mScene.PointerUp, e);
            this.PaintSurface += OnPaintSurface;
        }

        // The "shadow" state is our main-thread copy of the animation parameters. The actual scene
        // object lives on the render thread.
        private string mShadowSourceFilename = "";
        private string mShadowArtboardName = "";
        private string mShadowStateMachineName = "";
        private string mShadowAnimationName = "";

        private CancellationTokenSource mActiveSourceFileLoader = null;

        // Filename of the .riv file to open.
        public string Source
        {
            get => mShadowSourceFilename;
            set
            {
                if (mShadowSourceFilename == value)
                {
                    return;
                }
                mShadowSourceFilename = value;
                // Clear the current Scene while we wait for the new one to load.
                mSceneActionsQueue.Enqueue(() => mScene = new Scene());
                if (mActiveSourceFileLoader != null)
                    mActiveSourceFileLoader.Cancel();
                mActiveSourceFileLoader = new CancellationTokenSource();
                // Defer state machine inputs here until the new file is loaded.
                mDeferredSMInputsDuringFileLoad = new List<Action>();
                LoadSourceFileDataAsync(mActiveSourceFileLoader.Token);
            }
        }

        private async void LoadSourceFileDataAsync(CancellationToken cancellationToken)
        {
            byte[] data = null;
            Uri uri;
            if (Uri.TryCreate(mShadowSourceFilename, UriKind.Absolute, out uri))
            {
                var client = new WebClient();
                data = await client.DownloadDataTaskAsync(uri);
            }
            else
            {
                var getFileTask = Package.Current.InstalledLocation.TryGetItemAsync(mShadowSourceFilename);
                var storageFile = await getFileTask as StorageFile;
                if (storageFile != null && !cancellationToken.IsCancellationRequested)
                {
                    var inputStream = await storageFile.OpenSequentialReadAsync();
                    var fileStream = inputStream.AsStreamForRead();
                    data = new byte[fileStream.Length];
                    fileStream.Read(data, 0, data.Length);
                    fileStream.Dispose();  // Don't keep the file open.
                }
            }
            if (data != null && !cancellationToken.IsCancellationRequested)
            {
                mSceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.File, data));
                // Apply deferred state machine inputs once the scene is fully loaded.
                foreach (Action stateMachineInput in mDeferredSMInputsDuringFileLoad)
                    mSceneActionsQueue.Enqueue(stateMachineInput);
            }
            mDeferredSMInputsDuringFileLoad = null;
            mActiveSourceFileLoader = null;
        }

        // Name of the artbord to load from the .riv file. If null or empty, the default artboard
        // will be loaded.
        public string Artboard
        {
            get => mShadowArtboardName;
            set
            {
                if (mShadowArtboardName == value)
                {
                    return;
                }
                mShadowArtboardName = value;
                mSceneActionsQueue.Enqueue(() => mArtboardName = value);
                if (mActiveSourceFileLoader != null)
                {
                    // If a file is currently loading async, it will apply mShadowArtboardName once
                    // it completes. Loading a new artboard also invalidates any state machine
                    // inputs that were waiting for the file load.
                    mDeferredSMInputsDuringFileLoad.Clear();
                }
                else
                {
                    mSceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.Artboard));
                }
            }
        }

        // Name of the state machine to load from the .riv file.
        public string StateMachine
        {
            get => mShadowStateMachineName;
            set
            {
                if (mShadowStateMachineName == value)
                {
                    return;
                }
                mShadowStateMachineName = value;
                mSceneActionsQueue.Enqueue(() => mStateMachineName = value);
                if (mActiveSourceFileLoader != null)
                {
                    // If a file is currently loading async, it will apply mShadowStateMachineName
                    // once it completes. Loading a new state machine also invalidates any state
                    // machine inputs that were waiting for the file load.
                    mDeferredSMInputsDuringFileLoad.Clear();
                }
                else
                {
                    mSceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.AnimationOrStateMachine));
                }
            }
        }

        // Name of the fallback animation to load from the .riv if StateMachine is null or empty.
        public string Animation
        {
            get => mShadowAnimationName;
            set
            {
                if (mShadowAnimationName == value)
                {
                    return;
                }
                mShadowAnimationName = value;
                mSceneActionsQueue.Enqueue(() => mAnimationName = value);
                // If a file is currently loading async, it will apply mAnimationName once it
                // completes.
                if (mActiveSourceFileLoader == null)
                {
                    mSceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.AnimationOrStateMachine));
                }
            }
        }

        // State machine inputs to set once the current async file load finishes.
        private List<Action> mDeferredSMInputsDuringFileLoad = null;

        private void EnqueueStateMachineInput(Action stateMachineInput)
        {
            if (mDeferredSMInputsDuringFileLoad != null)
            {
                // A source file is currently loading async. Don't set this input until it completes.
                mDeferredSMInputsDuringFileLoad.Add(stateMachineInput);
            }
            else
            {
                mSceneActionsQueue.Enqueue(stateMachineInput);
            }
        }

        public void SetBool(string name, bool value)
        {
            EnqueueStateMachineInput(() => mScene.SetBool(name, value));
        }

        public void SetNumber(string name, float value)
        {
            EnqueueStateMachineInput(() => mScene.SetNumber(name, value));
        }

        public void FireTrigger(string name)
        {
            EnqueueStateMachineInput(() => mScene.FireTrigger(name));
        }

        private delegate void PointerHandler(Vec2D pos);

        private void HandlePointerEvent(PointerHandler handler, PointerRoutedEventArgs e)
        {
            if (mActiveSourceFileLoader != null)
            {
                // Ignore pointer events while a new scene is loading.
                return;
            }

            // Capture the viewSize and pointerPos at the time of the event.
            var viewSize = this.ActualSize;
            var pointerPos = e.GetCurrentPoint(this).Position;

            // Forward the pointer event to the render thread.
            mSceneActionsQueue.Enqueue(() =>
            {
                Mat2D mat = ComputeAlignment(viewSize.X, viewSize.Y);
                Mat2D inverse;
                if (mat.Invert(out inverse))
                {
                    Vec2D artboardPos = inverse * new Vec2D((float)pointerPos.X, (float)pointerPos.Y);
                    handler(artboardPos);
                }
            });
        }

        // Incremented when the "InvalLoop" (responsible for scheduling PaintSurface events) should
        // terminate.
        int mInvalLoopContinuationToken = 0;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Window.Current.VisibilityChanged += (object s, VisibilityChangedEventArgs vce) =>
            {
                ++mInvalLoopContinuationToken;  // Terminate the existing inval loop (if any).
                if (vce.Visible)
                {
                    InvalLoopAsync(mInvalLoopContinuationToken);
                }
            };
        }

        // Schedules continual PaintSurface events at 120fps until the window is no longer visible.
        // (Multiple calls to Invalidate() between PaintSurface events are coalesced.)
        private async void InvalLoopAsync(int continuationToken)
        {
            while (continuationToken == mInvalLoopContinuationToken)
            {
                this.Invalidate();
                await Task.Delay(TimeSpan.FromMilliseconds(8));  // 120 fps
            }
        }

        // mScene is used on the render thread exclusively.
        Scene mScene = new Scene();

        // Source actions originating from other threads must be funneled through this queue.
        ConcurrentQueue<Action> mSceneActionsQueue = new ConcurrentQueue<Action>();

        // This is the render-thread copy of the animation parameters. They are set via
        // mSceneActionsQueue. mScene is then blah blah blah
        private string mArtboardName;
        private string mAnimationName;
        private string mStateMachineName;

        private enum SceneUpdates
        {
            File = 3,
            Artboard = 2,
            AnimationOrStateMachine = 1,
        };

        DateTime mLastPaintTime;

        private void OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            // Handle pending scene actions from the main thread.
            Action action;
            while (mSceneActionsQueue.TryDequeue(out action))
            {
                action();
            }

            if (!mScene.IsLoaded)
            {
                return;
            }

            // Run the animation.
            var now = DateTime.Now;
            if (mLastPaintTime != null)
            {
                mScene.AdvanceAndApply((now - mLastPaintTime).TotalSeconds);
            }
            mLastPaintTime = now;

            // Render.
            e.Surface.Canvas.Clear();
            var renderer = new Renderer(e.Surface.Canvas);
            renderer.Save();
            renderer.Transform(ComputeAlignment(e.BackendRenderTarget.Width, e.BackendRenderTarget.Height));
            mScene.Draw(renderer);
            renderer.Restore();
        }

        // Called from the render thread. Updates mScene according to updates.
        void UpdateScene(SceneUpdates updates, byte[] sourceFileData = null)
        {
            if (updates >= SceneUpdates.File)
            {
                mScene.LoadFile(sourceFileData);
            }
            if (updates >= SceneUpdates.Artboard)
            {
                mScene.LoadArtboard(mArtboardName);
            }
            if (updates >= SceneUpdates.AnimationOrStateMachine)
            {
                if (!String.IsNullOrEmpty(mStateMachineName))
                {
                    mScene.LoadStateMachine(mStateMachineName);
                }
                else if (!String.IsNullOrEmpty(mAnimationName))
                {
                    mScene.LoadAnimation(mAnimationName);
                }
                else
                {
                    if (!mScene.LoadStateMachine(null))
                        mScene.LoadAnimation(null);
                }
            }
        }

        // Called from the render thread. Computes alignment based on the size of mScene.
        private Mat2D ComputeAlignment(double width, double height)
        {
            return ComputeAlignment(new AABB(0, 0, (float)width, (float)height));
        }

        // Called from the render thread. Computes alignment based on the size of mScene.
        private Mat2D ComputeAlignment(AABB frame)
        {
            return Renderer.ComputeAlignment(Fit.Contain, Alignment.Center, frame,
                                             new AABB(0, 0, mScene.Width, mScene.Height));
        }
    }
}
