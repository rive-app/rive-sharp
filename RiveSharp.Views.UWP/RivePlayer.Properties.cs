using System;
using System.Collections.Generic;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace RiveSharp.Views
{
    // XAML properies for RivePlayer.
    [ContentProperty(Name = nameof(StateMachineInputs))]
    public partial class RivePlayer
    {
        // Filename of the .riv file to open. Can be a file path or a URL.
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(string),
            typeof(RivePlayer),
            new PropertyMetadata(null, OnSourceNameChanged)
        );

        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        // Name of the artbord to load from the .riv file. If null or empty, the default artboard
        // will be loaded.
        public static readonly DependencyProperty ArtboardProperty = DependencyProperty.Register(
            nameof(Artboard),
            typeof(string),
            typeof(RivePlayer),
            new PropertyMetadata(null, OnArtboardNameChanged)
        );

        public string Artboard
        {
            get => (string)GetValue(ArtboardProperty);
            set => SetValue(ArtboardProperty, value);
        }

        // Name of the state machine to load from the .riv file.
        public static readonly DependencyProperty StateMachineProperty = DependencyProperty.Register(
            nameof(StateMachine),
            typeof(string),
            typeof(RivePlayer),
            new PropertyMetadata(null, OnStateMachineNameChanged)
        );

        public string StateMachine
        {
            get => (string)GetValue(StateMachineProperty);
            set => SetValue(StateMachineProperty, value);
        }

        // Name of the fallback animation to load from the .riv if StateMachine is null or empty.
        public static readonly DependencyProperty AnimationProperty = DependencyProperty.Register(
            nameof(Animation),
            typeof(string),
            typeof(RivePlayer),
            new PropertyMetadata(null, OnAnimationNameChanged)
        );

        public string Animation
        {
            get => (string)GetValue(AnimationProperty);
            set => SetValue(AnimationProperty, value);
        }

        public static readonly DependencyProperty StateMachineInputsProperty = DependencyProperty.Register(
            nameof(StateMachineInputs),
            typeof(StateMachineInputCollection),
            typeof(RivePlayer),
            new PropertyMetadata(null)
        );

        public StateMachineInputCollection StateMachineInputs
        {
            get => (StateMachineInputCollection)GetValue(StateMachineInputsProperty);
            set => SetValue(StateMachineInputsProperty, value);
        }

        private static void OnSourceNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var player = (RivePlayer)d;
            var newSourceName = (string)e.NewValue;
            // Clear the current Scene while we wait for the new one to load.
            player.mSceneActionsQueue.Enqueue(() => player.mScene = new Scene());
            if (player.mActiveSourceFileLoader != null)
                player.mActiveSourceFileLoader.Cancel();
            player.mActiveSourceFileLoader = new CancellationTokenSource();
            // Defer state machine inputs here until the new file is loaded.
            player.mDeferredSMInputsDuringFileLoad = new List<Action>();
            player.LoadSourceFileDataAsync(newSourceName, player.mActiveSourceFileLoader.Token);
        }

        private static void OnArtboardNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var player = (RivePlayer)d;
            var newArtboardName = (string)e.NewValue;
            player.mSceneActionsQueue.Enqueue(() => player.mArtboardName = newArtboardName);
            if (player.mActiveSourceFileLoader != null)
            {
                // If a file is currently loading async, it will apply the new artboard once
                // it completes. Loading a new artboard also invalidates any state machine
                // inputs that were waiting for the file load.
                player.mDeferredSMInputsDuringFileLoad.Clear();
            }
            else
            {
                player.mSceneActionsQueue.Enqueue(() => player.UpdateScene(SceneUpdates.Artboard));
            }
        }

        private static void OnStateMachineNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var player = (RivePlayer)d;
            var newStateMachineName = (string)e.NewValue;
            player.mSceneActionsQueue.Enqueue(() => player.mStateMachineName = newStateMachineName);
            if (player.mActiveSourceFileLoader != null)
            {
                // If a file is currently loading async, it will apply the new state machine
                // once it completes. Loading a new state machine also invalidates any state
                // machine inputs that were waiting for the file load.
                player.mDeferredSMInputsDuringFileLoad.Clear();
            }
            else
            {
                player.mSceneActionsQueue.Enqueue(() => player.UpdateScene(SceneUpdates.AnimationOrStateMachine));
            }
        }

        private static void OnAnimationNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var player = (RivePlayer)d;
            var newAnimationName = (string)e.NewValue;
            player.mSceneActionsQueue.Enqueue(() => player.mAnimationName = newAnimationName);
            // If a file is currently loading async, it will apply the new animation once it completes.
            if (player.mActiveSourceFileLoader == null)
            {
                player.mSceneActionsQueue.Enqueue(() => player.UpdateScene(SceneUpdates.AnimationOrStateMachine));
            }
        }

    }
}
