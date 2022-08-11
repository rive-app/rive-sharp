// Copyright 2022 Rive

using System;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

namespace RiveSharp.Views
{
    // Manages a collection of StateMachineInput objects for RivePlayer. The [ContentProperty] tag
    // on RivePlayer instructs the XAML engine automatically route nested inputs through this
    // collection:
    //
    //   <rive:RivePlayer Source="...">
    //       <rive:BoolInput Target=... />
    //   </rive:RivePlayer>
    //
    public class StateMachineInputCollection : DependencyObjectCollection
    {
        private readonly WeakReference<RivePlayer> rivePlayer;

        public StateMachineInputCollection(RivePlayer rivePlayer)
        {
            this.rivePlayer = new WeakReference<RivePlayer>(rivePlayer);
            VectorChanged += InputsVectorChanged;
        }

        private void InputsVectorChanged(IObservableVector<DependencyObject> sender,
                                         IVectorChangedEventArgs @event)
        {
            switch (@event.CollectionChange)
            {
                case CollectionChange.ItemInserted:
                case CollectionChange.ItemChanged:
                {
                    var input = (StateMachineInput)sender[(int)@event.Index];
                    input.SetRivePlayer(rivePlayer);
                }
                break;
                case CollectionChange.ItemRemoved:
                {
                    var input = (StateMachineInput)sender[(int)@event.Index];
                    input.SetRivePlayer(new WeakReference<RivePlayer>(null));
                    break;
                }
                case CollectionChange.Reset:
                    foreach (StateMachineInput input in sender)
                    {
                        input.SetRivePlayer(new WeakReference<RivePlayer>(null));
                    }
                    break;
            }
        }
    }
}
