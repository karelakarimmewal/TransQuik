using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Get the current process
            Process currentProcess = Process.GetCurrentProcess();

            // Set the priority to High
            currentProcess.PriorityClass = ProcessPriorityClass.High;
        }
    }
}
