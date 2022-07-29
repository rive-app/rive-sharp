using System;
using Windows.UI.Xaml;

namespace RiveSharp.Views
{
    // Wraps a custom, named state machine input value. This element is not visible and does not
    // participate in layout, but it is designed to live in the XAML tree and apply its Value to its
    // parent "RivePlayer" object's Scene.
    public abstract class StateMachineInput : FrameworkElement
    {
        public StateMachineInput()
        {
            this.Visibility = Visibility.Collapsed;
            this.Loaded += (object s, RoutedEventArgs e) => Apply();
        }

        private string mInputName;
        public string InputName
        {
            get => mInputName;
            set
            {
                mInputName = value;
                Apply();
            }
        }

        protected bool GetParentPanel(out RivePlayer parent)
        {
            if (this.Parent == null)
            {
                if (this.IsLoaded)
                    throw new Exception("StateMachineInput has a null parent after being loaded.");
                parent = null;
                return false;
            }
            if (this.Parent.GetType() != typeof(RivePlayer))
                throw new Exception("StateMachineInput must be a direct child of RivePlayer.");
            parent = this.Parent as RivePlayer;
            return true;
        }

        protected void Apply()
        {
            RivePlayer parent;
            if (GetParentPanel(out parent))
                Apply(parent);
        }

        protected abstract void Apply(RivePlayer parent);
    }

    public class StateMachineInputBool : StateMachineInput
    {
        // Define "Value" as a DependencyProperty so it can be data-bound.
        public static readonly DependencyProperty sValueProperty = DependencyProperty.Register(
            "Value",
            typeof(bool),
            typeof(StateMachineInputBool),
            new PropertyMetadata(false, new PropertyChangedCallback(OnValueChanged))
        );

        public static DependencyProperty ValueProperty => sValueProperty;

        public bool Value
        {
            get => (bool)GetValue(sValueProperty);
            set => SetValue(sValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var smib = d as StateMachineInputBool;
            if (smib != null)
                smib.Apply();
        }

        protected override void Apply(RivePlayer parent)
        {
            parent.SetBool(this.InputName, this.Value);
        }
    }

    public class StateMachineInputNumber : StateMachineInput
    {
        // Define "Value" as a DependencyProperty so it can be data-bound.
        private static readonly DependencyProperty sValueProperty = DependencyProperty.Register(
            "Value",
            typeof(double),
            typeof(StateMachineInputNumber),
            new PropertyMetadata(0.0, new PropertyChangedCallback(OnValueChanged))
        );

        public static DependencyProperty ValueProperty => sValueProperty;

        public double Value
        {
            get => (double)GetValue(sValueProperty);
            set => SetValue(sValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var smin = d as StateMachineInputNumber;
            if (smin != null)
                smin.Apply();
        }

        protected override void Apply(RivePlayer parent)
        {
            parent.SetNumber(this.InputName, (float)this.Value);
        }
    }

    public class StateMachineInputTrigger : StateMachineInput
    {
        public void Fire()
        {
            RivePlayer parent;
            if (GetParentPanel(out parent))
                parent.FireTrigger(this.InputName);
        }

        // Make a Fire() overload that matches the RoutedEventHandler delegate.
        // This allows us do to things like <Button Click="MyTriggerInput.Fire" ... />
        public void Fire(object s, RoutedEventArgs e) => Fire();

        // Triggers don't have anything to apply.
        protected override void Apply(RivePlayer parent) { }
    }
}