using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TranQuik.Configuration
{
    public class Config
    {
        private const string ConfigFileName = "AppSettings.json";
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", ConfigFileName);

        // Method to read and synchronize settings with application properties
        public static void LoadAppSettings()
        {
            try
            {
                EnsureConfigDirectoryAndFile(); // Ensure the directory and file exist

                Dictionary<string, string> appSettings = new Dictionary<string, string>();

                string json = File.ReadAllText(ConfigFilePath);
                appSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                // Update application properties with loaded settings
                UpdateAppSettings(appSettings);
                UpdateDatabaseSettings(appSettings);

                // Save the loaded settings back to file to ensure consistency
                SavedSettings.SaveAppSettings();
                SaveAppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading AppSettings: {ex.Message}");
            }
        }

        private static void EnsureConfigDirectoryAndFile()
        {
            string directory = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(ConfigFilePath))
            {
                // Create a new AppSettings.json file with default values
                CreateDefaultAppSettingsFile();
            }
        }

        private static void CreateDefaultAppSettingsFile()
        {
            Dictionary<string, string> defaultSettings = new Dictionary<string, string>
            {
                { "_AppFontSize", "18" },
                { "_AppFontFamily", "Arial" },
                { "_AppSaleMode", "3" },
                { "_AppID", "Development" },
                { "_AppSecMonitor", "False" },
                { "_AppAllowImage", "True" },
                { "_AppStatus", "False" },
                { "_LocalDbServer", "localhost" },
                { "_LocalDbPort", "3308" },
                { "_LocalDbUser", "vtecPOS" },
                { "_LocalDbPassword", "vtecpwnet" },
                { "_LocalDbName", "vtectestaw" },
                { "_CloudDbServer", "" },
                { "_CloudDbPort", "0" },
                { "_CloudDbUser", "" },
                { "_CloudDbPassword", "" },
                { "_CloudDbName", "" }
            };

            string json = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }

        private static void UpdateAppSettings(Dictionary<string, string> appSettings)
        {
            AppSettings.AppFontSize = GetSettingInt(appSettings, "_AppFontSize", AppSettings.AppFontSize);
            AppSettings.AppFontFamily = GetSettingString(appSettings, "_AppFontFamily", AppSettings.AppFontFamily);
            AppSettings.AppSaleMode = GetSettingInt(appSettings, "_AppSaleMode", AppSettings.AppSaleMode);
            AppSettings.AppID = GetSettingString(appSettings, "_AppID", AppSettings.AppID);
            AppSettings.AppSecMonitor = GetSettingBool(appSettings, "_AppSecMonitor", AppSettings.AppSecMonitor);
            AppSettings.AppAllowImage = GetSettingBool(appSettings, "_AppAllowImage", AppSettings.AppAllowImage);
            AppSettings.AppStatus = GetSettingBool(appSettings, "_AppStatus", AppSettings.AppStatus);
        }

        private static void UpdateDatabaseSettings(Dictionary<string, string> appSettings)
        {
            DatabaseSettings.LocalDbServer = GetSettingString(appSettings, "_LocalDbServer", DatabaseSettings.LocalDbServer);
            DatabaseSettings.LocalDbPort = GetSettingInt(appSettings, "_LocalDbPort", DatabaseSettings.LocalDbPort);
            DatabaseSettings.LocalDbUser = GetSettingString(appSettings, "_LocalDbUser", DatabaseSettings.LocalDbUser);
            DatabaseSettings.LocalDbPassword = GetSettingString(appSettings, "_LocalDbPassword", DatabaseSettings.LocalDbPassword);
            DatabaseSettings.LocalDbName = GetSettingString(appSettings, "_LocalDbName", DatabaseSettings.LocalDbName);

            DatabaseSettings.CloudDbServer = GetSettingString(appSettings, "_CloudDbServer", DatabaseSettings.CloudDbServer);
            DatabaseSettings.CloudDbPort = GetSettingInt(appSettings, "_CloudDbPort", DatabaseSettings.CloudDbPort);
            DatabaseSettings.CloudDbUser = GetSettingString(appSettings, "_CloudDbUser", DatabaseSettings.CloudDbUser);
            DatabaseSettings.CloudDbPassword = GetSettingString(appSettings, "_CloudDbPassword", DatabaseSettings.CloudDbPassword);
            DatabaseSettings.CloudDbName = GetSettingString(appSettings, "_CloudDbName", DatabaseSettings.CloudDbName);
        }

        private static int GetSettingInt(Dictionary<string, string> settings, string key, int defaultValue)
        {
            if (settings.TryGetValue(key, out string value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        private static string GetSettingString(Dictionary<string, string> settings, string key, string defaultValue)
        {
            if (settings.TryGetValue(key, out string value))
            {
                return value;
            }
            return defaultValue;
        }

        private static bool GetSettingBool(Dictionary<string, string> settings, string key, bool defaultValue)
        {
            if (settings.TryGetValue(key, out string value) && bool.TryParse(value, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        // Method to save current application settings to AppSettings.json
        public static void SaveAppSettings()
        {
            try
            {
                Dictionary<string, string> appSettings = new Dictionary<string, string>
                {
                    { "_AppFontSize", AppSettings.AppFontSize.ToString() },
                    { "_AppFontFamily", AppSettings.AppFontFamily },
                    { "_AppSaleMode", AppSettings.AppSaleMode.ToString() },
                    { "_AppID", AppSettings.AppID },
                    { "_AppSecMonitor", AppSettings.AppSecMonitor.ToString() },
                    { "_AppAllowImage", AppSettings.AppAllowImage.ToString() },
                    { "_AppStatus", AppSettings.AppStatus.ToString() },

                    { "_LocalDbServer", DatabaseSettings.LocalDbServer },
                    { "_LocalDbPort", DatabaseSettings.LocalDbPort.ToString() },
                    { "_LocalDbUser", DatabaseSettings.LocalDbUser },
                    { "_LocalDbPassword", DatabaseSettings.LocalDbPassword },
                    { "_LocalDbName", DatabaseSettings.LocalDbName },

                    { "_CloudDbServer", DatabaseSettings.CloudDbServer },
                    { "_CloudDbPort", DatabaseSettings.CloudDbPort.ToString() },
                    { "_CloudDbUser", DatabaseSettings.CloudDbUser },
                    { "_CloudDbPassword", DatabaseSettings.CloudDbPassword },
                    { "_CloudDbName", DatabaseSettings.CloudDbName }
                };

                string json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);

                Console.WriteLine("AppSettings updated and saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving AppSettings: {ex.Message}");
            }
        }
    }
}
