using System.Data.SqlClient;
using TranQuik.Configuration;

public class CloudDbConnector
{
    public SqlConnection GetSqlConnection()
    {
        string connectionString = $"Server={DatabaseSettings.CloudDbServer},{DatabaseSettings.CloudDbPort};" +
                                   $"Database={DatabaseSettings.CloudDbName};" +
                                   $"User Id={DatabaseSettings.CloudDbUser};" +
                                   $"Password={DatabaseSettings.CloudDbPassword};";

        SqlConnection connection = new SqlConnection(connectionString);
        return connection;
    }
}
