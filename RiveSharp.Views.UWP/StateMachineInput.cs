// Copyright 2022 Rive

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace RiveSharp.Views
{
    // This base class wraps a custom, named state machine input value.
    public abstract class StateMachineInput : DependencyObject
    {
        private string _target;
        public string Target
        {
            get => _target;  // Must be null-checked before use.
            set
            {
                _target = value;
                Apply();
            }
        }

        private WeakReference<RivePlayer> _rivePlayer = new WeakReference<RivePlayer>(null);
        protected WeakReference<RivePlayer> RivePlayer => _rivePlayer;

        // Sets _rivePlayer to the given rivePlayer object and applies our input value to the state
        // machine. Does nothing if _rivePlayer was already equal to rivePlayer.
        internal void SetRivePlayer(WeakReference<RivePlayer> rivePlayer)
        {
            _rivePlayer = rivePlayer;
            Apply();
        }

        protected void Apply()
        {
            if (!String.IsNullOrEmpty(_target) && _rivePlayer.TryGetTarget(out var rivePlayer))
            {
                Apply(rivePlayer, _target);
            }
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
            if (!String.IsNullOrEmpty(this.Target) && this.RivePlayer.TryGetTarget(out var rivePlayer))
            {
                rivePlayer.FireTrigger(this.Target);
            }
        }

        // Make a Fire() overload that matches the RoutedEventHandler delegate.
        // This allows us do to things like <Button Click="MyTriggerInput.Fire" ... />
        public void Fire(object s, RoutedEventArgs e) => Fire();

        // Triggers don't have any persistent data to apply.
        protected override void Apply(RivePlayer rivePlayer, string inputName) { }
    }
}
