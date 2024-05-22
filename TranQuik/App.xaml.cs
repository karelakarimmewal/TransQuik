using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using TranQuik.Configuration;

namespace TranQuik
{
    public partial class App : Application
    {
        public App()
        {
            // Set the default culture to Indonesian (id-ID)
            CultureInfo indonesianCulture = new CultureInfo("id-ID");
            CultureInfo.DefaultThreadCurrentCulture = indonesianCulture;
            CultureInfo.DefaultThreadCurrentUICulture = indonesianCulture;

            ConfigurationLogging configLogging = new ConfigurationLogging();
            configLogging.ConfigureLogging();
            Log.ForContext("LogType", "ApplicationLog").Information($"Application Started");
        }
        private void MyEventHandler(object sender, TouchEventArgs e)
        {
            // Your event handling logic here
            // For example, you can log the touch event
            Log.Information($"Touch event occurred on element: {sender}");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Get the current process
            Process currentProcess = Process.GetCurrentProcess();

            // Set the priority to High 
            currentProcess.PriorityClass = ProcessPriorityClass.High;
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.TouchDownEvent, new EventHandler<TouchEventArgs>(MyEventHandler));
        }
    }
}
