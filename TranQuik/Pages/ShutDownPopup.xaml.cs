using System;
using System.Windows;

namespace TranQuik.Pages
{
    public partial class ShutDownPopup : Window
    {
        public ShutDownPopup()
        {
            InitializeComponent();
        }

        private void ShutdownButton(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
