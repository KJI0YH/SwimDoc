using DataLayer.EfCore;
using ServiceLayer;

namespace ServiceLayer.ConnectionService;

public class DatabaseConnectionService : IDatabaseConnection
{
    private string _connection = $"Data Source={ApplicationPaths.GetUserDataFilePath("Default.swimdb")}";
    public string? CurrentConnection()
    {
        return _connection;
    }

    public event Action<string>? ConnectionChanged;

    public void SetConnection(string connectionString)
    {
        if (string.Equals(_connection, connectionString, StringComparison.Ordinal))
            return;
        _connection = connectionString;
        ConnectionChanged?.Invoke(connectionString);
    }
}
