using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using TranQuik.Configuration;

namespace TranQuik
{
    public partial class App : Application
    {
        private const string MutexName = "TranQuikApplicationMutex";
        private Mutex singleInstanceMutex;

        public App()
        {
            // Set the default culture to Indonesian (id-ID)
            CultureInfo indonesianCulture = new CultureInfo("id-ID");
            CultureInfo.DefaultThreadCurrentCulture = indonesianCulture;
            CultureInfo.DefaultThreadCurrentUICulture = indonesianCulture;

            ConfigurationLogging configLogging = new ConfigurationLogging();
            configLogging.ConfigureLogging();

            singleInstanceMutex = new Mutex(true, MutexName, out bool isFirstInstance);

            if (!isFirstInstance)
            {
                // Show a message box indicating that another instance is already running
                MessageBox.Show("Another instance of the application is already running. Please wait or close the existing instance.",
                    "Application Already Running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Terminate this instance
                Environment.Exit(0);
            }

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

        protected override void OnExit(ExitEventArgs e)
        {
            // Release the mutex on exit
            singleInstanceMutex?.Close();
            base.OnExit(e);
        }
    }
}
