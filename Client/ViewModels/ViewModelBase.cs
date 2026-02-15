/*
* FILE            : ViewModelBase.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-14
* DESCRIPTION     :
*   Base ViewModel with property change notification for WPF bindings.
*/

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WordGameClient.ViewModels
{
    /*
    * CLASS        : ViewModelBase
    * DESCRIPTION  :
    *   Implements INotifyPropertyChanged so UI updates when properties change.
    */
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /*
        * METHOD       : NotifyPropertyChanged
        * DESCRIPTION  :
        *   Raises PropertyChanged event for a property.
        * PARAMETERS   :
        *   string propertyName : property name
        * RETURNS      : NONE
        */
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
