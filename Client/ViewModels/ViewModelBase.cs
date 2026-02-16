/*
* FILE            : ViewModelBase.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen & Ritik Vyas
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   Base ViewModel with property change notification for WPF bindings.
*/

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WordGameClient.ViewModels
{
    //
    // CLASS : ViewModelBase
    // DESCRIPTION : Implements INotifyPropertyChanged so UI updates when properties change.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        //
        // FUNCTION : NotifyPropertyChanged
        // DESCRIPTION : Raises the PropertyChanged event for the specified property.
        // PARAMETERS : string propertyName - the name of the property that changed (automatically filled by CallerMemberName)
        // RETURNS : void
        //
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler? handler = this.PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }

            return;
        }
    }
}
