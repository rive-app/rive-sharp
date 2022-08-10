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
    // Implements a simple view that plays content from a .riv file.
    public partial class RivePlayer : SKSwapChainPanel
    {
        private CancellationTokenSource mActiveSourceFileLoader = null;

        public RivePlayer()
        {
            this.StateMachineInputs = new StateMachineInputCollection(this);
            this.Loaded += OnLoaded;
            this.PointerPressed +=
                (object s, PointerRoutedEventArgs e) => HandlePointerEvent(mScene.PointerDown, e);
            this.PointerMoved +=
                (object s, PointerRoutedEventArgs e) => HandlePointerEvent(mScene.PointerMove, e);
            this.PointerReleased +=
                (object s, PointerRoutedEventArgs e) => HandlePointerEvent(mScene.PointerUp, e);
            this.PaintSurface += OnPaintSurface;
        }

        private async void LoadSourceFileDataAsync(string name, CancellationToken cancellationToken)
        {
            byte[] data = null;
            Uri uri;
            if (Uri.TryCreate(name, UriKind.Absolute, out uri))
            {
                var client = new WebClient();
                data = await client.DownloadDataTaskAsync(uri);
            }
            else
            {
                var getFileTask = Package.Current.InstalledLocation.TryGetItemAsync(name);
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
