using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace TranQuik.Configuration
{
    public class ConfigurationLogging
    {
        public void ConfigureLogging()
        {
            Console.WriteLine("Application Started");
            string baseLogDirectory = Path.Combine(Environment.CurrentDirectory, "Logs");

            // Create the base log directory if it doesn't exist
            if (!Directory.Exists(baseLogDirectory))
            {
                Directory.CreateDirectory(baseLogDirectory);
            }

            ConfigureLogger(baseLogDirectory);
        }

        private void ConfigureLogger(string baseLogDirectory)
        {
            // Create subdirectories for each log type
            CreateLogDirectory(baseLogDirectory, "UserLog");
            CreateLogDirectory(baseLogDirectory, "SyncLog");
            CreateLogDirectory(baseLogDirectory, "TransactionLog");
            CreateLogDirectory(baseLogDirectory, "ApplicationLog");

            // Configure Serilog to write logs to different directories based on LogType
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(evt => GetLogType(evt) == "UserLog")
                    .WriteTo.File(Path.Combine(baseLogDirectory, "UserLog", "UserLog.txt"), rollingInterval: RollingInterval.Day)) // Log for users
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(evt => GetLogType(evt) == "SyncLog")
                    .WriteTo.File(Path.Combine(baseLogDirectory, "SyncLog", "SyncLog.txt"), rollingInterval: RollingInterval.Day)) // Log for synchronization
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(evt => GetLogType(evt) == "TransactionLog")
                    .WriteTo.File(Path.Combine(baseLogDirectory, "TransactionLog", "TransactionLog.txt"), rollingInterval: RollingInterval.Day)) // Log for transactions
                .WriteTo.Logger(l => l.Filter.ByIncludingOnly(evt => GetLogType(evt) == "ApplicationLog")
                    .WriteTo.File(Path.Combine(baseLogDirectory, "ApplicationLog", "ApplicationLog.txt"), rollingInterval: RollingInterval.Day)) // Log for application
                .CreateLogger();
        }

        private string GetLogType(LogEvent evt)
        {
            if (evt.Properties.TryGetValue("LogType", out var logTypeValue) && logTypeValue is ScalarValue scalarValue && scalarValue.Value is string logType)
            {
                return logType;
            }

            return string.Empty; // Default to empty string if LogType property is not found or not a string
        }


        private void CreateLogDirectory(string baseLogDirectory, string logType)
        {
            string logDirectory = Path.Combine(baseLogDirectory, logType);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }
    }
}
