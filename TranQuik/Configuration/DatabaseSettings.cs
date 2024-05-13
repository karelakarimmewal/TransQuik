namespace TranQuik.Configuration
{
    internal static class DatabaseSettings
    {
        // Local Database Connection Settings (MySQL)
        public static string LocalDbServer { get; set; }
        public static int LocalDbPort { get; set; }
        public static string LocalDbUser { get; set; }
        public static string LocalDbPassword { get; set; }
        public static string LocalDbName { get; set; }

        // Cloud Database Connection Settings (Microsoft SQL Server)
        public static string CloudDbServer { get; set; }
        public static int CloudDbPort { get; set; }
        public static string CloudDbUser { get; set; }
        public static string CloudDbPassword { get; set; }
        public static string CloudDbName { get; set; }
    }

}
