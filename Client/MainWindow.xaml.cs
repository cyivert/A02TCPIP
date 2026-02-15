/*
* FILE            : MainWindow.xaml.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   Main window code-behind. Wires MVVM DataContext.
*/

using System.Windows;
using WordGameClient.ViewModels;

namespace WordGameClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainWindowViewModel(App.Settings, App.ConfigurationErrorMessage);

            return;
        }
    }
}
