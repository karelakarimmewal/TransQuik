using Serilog;
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
            Log.ForContext("LogType", "ApplicationLog").Information("Application Stopped");
            Environment.Exit(0);
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
