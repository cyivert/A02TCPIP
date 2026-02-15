/*
* FILE            : ViewModelBase.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-12
* DESCRIPTION     :
*   Base ViewModel implementing INotifyPropertyChanged. Providing property change notification for WPF data binding.
*/

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WordGameClient.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler? handler = this.PropertyChanged;

            if (handler != null)
            {
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            return;
        }
    }
}
