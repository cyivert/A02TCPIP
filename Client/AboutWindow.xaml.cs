/*
* FILE            : AboutWindow.xaml.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   About dialog window displaying application info and team members.
*/

using System.Windows;

namespace WordGameClient
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            return;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            return;
        }
    }
}
