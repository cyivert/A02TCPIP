/*
* FILE            : AboutWindow.xaml.cs
* PROJECT         : A02TCPIP
* PROGRAMMER      : Tuan Thanh Nguyen
* FIRST VERSION   : 2026-02-15
* DESCRIPTION     :
*   About window displaying application and team information.
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            return;
        }
    }
}