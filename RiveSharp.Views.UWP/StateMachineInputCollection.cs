using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private WeakReference<RivePlayer> mRivePlayer;

        public StateMachineInputCollection(RivePlayer rivePlayer)
        {
            mRivePlayer = new WeakReference<RivePlayer>(rivePlayer);
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
                        input.SetRivePlayer(mRivePlayer);
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
