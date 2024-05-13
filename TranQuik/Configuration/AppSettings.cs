using System.Configuration;
using System;

namespace TranQuik.Configuration
{
    public static class AppSettings
    {
        public static int AppFontSize { get; set; }
        public static string AppFontFamily { get; set; }
        public static int AppSaleMode { get; set; }
        public static string AppID { get; set; }
        public static bool AppSecMonitor { get; set; }
        public static bool AppAllowImage { get; set; }
        public static bool AppStatus { get; set; }
    }

    public static class SavedSettings
    {
        public static void SaveAppSettings()
        {
            try
            {
                // Update Application.Properties.Settings with AppSettings values
                Properties.Settings.Default._AppFontSize = AppSettings.AppFontSize;
                Properties.Settings.Default._AppFontFamily = AppSettings.AppFontFamily;
                Properties.Settings.Default._AppSaleMode = AppSettings.AppSaleMode;
                Properties.Settings.Default._AppID = AppSettings.AppID;
                Properties.Settings.Default._AppSecMonitor = AppSettings.AppSecMonitor;
                Properties.Settings.Default._AppAllowImage = AppSettings.AppAllowImage;
                Properties.Settings.Default._AppStatus = AppSettings.AppStatus;

                // Save the updated settings
                Properties.Settings.Default.Save();
                Console.WriteLine("AppSettings updated and saved to Application.Properties.Settings.");
            }
            catch (ConfigurationErrorsException ex)
            {
                Console.WriteLine($"Error saving AppSettings: {ex.Message}");
            }
        }
    }
}
