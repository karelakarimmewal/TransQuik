using MySql.Data.MySqlClient;
using TranQuik.Configuration;

public class LocalDbConnector
{
    public MySqlConnection GetMySqlConnection()
    {
        string connectionString = $"Server={DatabaseSettings.LocalDbServer};Port={DatabaseSettings.LocalDbPort};" +
                                   $"Database={DatabaseSettings.LocalDbName};Uid={DatabaseSettings.LocalDbUser};" +
                                   $"Pwd={DatabaseSettings.LocalDbPassword};";

        MySqlConnection connection = new MySqlConnection(connectionString);
        return connection;
    }
}
