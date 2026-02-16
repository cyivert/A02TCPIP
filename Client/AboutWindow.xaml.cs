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
    //
    // CLASS : AboutWindow
    // DESCRIPTION : About dialog window that displays game information and team member names.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public partial class AboutWindow : Window
    {
        //
        // FUNCTION : AboutWindow (constructor)
        // DESCRIPTION : Initializes the AboutWindow by calling InitializeComponent to load the XAML.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public AboutWindow()
        {
            InitializeComponent();

            return;
        }

        //
        // FUNCTION : OkButton_Click
        // DESCRIPTION : Event handler for the OK button click. Closes the about window.
        // PARAMETERS : object sender - the source of the event (the button);
        //              RoutedEventArgs e - event data.
        // RETURNS : void
        //
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            return;
        }
    }
}
