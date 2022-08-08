using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace RiveSharp.Views
{
    // This base class wraps a custom, named state machine input value.
    public abstract class StateMachineInput : DependencyObject
    {
        private string mTarget;
        public string Target
        {
            get => mTarget;  // Must be null-checked before use.
            set
            {
                mTarget = value;
                Apply();
            }
        }

        private WeakReference<RivePlayer> mRivePlayer = new WeakReference<RivePlayer>(null);
        protected WeakReference<RivePlayer> RivePlayer => mRivePlayer;

        // Sets mRivePlayer to the given rivePlayer object and applies our input value to the state
        // machine. Does nothing if mRivePlayer was already equal to rivePlayer.
        internal void SetRivePlayer(WeakReference<RivePlayer> rivePlayer)
        {
            mRivePlayer = rivePlayer;
            Apply();
        }

        protected void Apply()
        {
            RivePlayer rivePlayer;
            if (!String.IsNullOrEmpty(mTarget) && mRivePlayer.TryGetTarget(out rivePlayer))
                Apply(rivePlayer, mTarget);
        }

        // Applies our input value to the rivePlayer's state machine.
        // rivePlayer and inputName are guaranteed to not be null or empty.
        protected abstract void Apply(RivePlayer rivePlayer, string inputName);
    }

    [ContentProperty(Name = nameof(Value))]
    public class BoolInput : StateMachineInput
    {
        // Define "Value" as a DependencyProperty so it can be data-bound.
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(bool),
            typeof(BoolInput),
            new PropertyMetadata(false, new PropertyChangedCallback(OnValueChanged))
        );

        public bool Value
        {
            get => (bool)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BoolInput)d).Apply();
        }

        protected override void Apply(RivePlayer rivePlayer, string inputName)
        {
            rivePlayer.SetBool(inputName, this.Value);
        }
    }

    [ContentProperty(Name = nameof(Value))]
    public class NumberInput : StateMachineInput
    {
        // Define "Value" as a DependencyProperty so it can be data-bound.
        private static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(NumberInput),
            new PropertyMetadata(0.0, new PropertyChangedCallback(OnValueChanged))
        );

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((NumberInput)d).Apply();
        }

        protected override void Apply(RivePlayer rivePlayer, string inputName)
        {
            rivePlayer.SetNumber(inputName, (float)this.Value);
        }
    }

    public class TriggerInput : StateMachineInput
    {
        public void Fire()
        {
            RivePlayer rivePlayer;
            if (!String.IsNullOrEmpty(this.Target) && this.RivePlayer.TryGetTarget(out rivePlayer))
                rivePlayer.FireTrigger(this.Target);
        }

        // Make a Fire() overload that matches the RoutedEventHandler delegate.
        // This allows us do to things like <Button Click="MyTriggerInput.Fire" ... />
        public void Fire(object s, RoutedEventArgs e) => Fire();

        // Triggers don't have any persistent data to apply.
        protected override void Apply(RivePlayer rivePlayer, string inputName) { }
    }
}
