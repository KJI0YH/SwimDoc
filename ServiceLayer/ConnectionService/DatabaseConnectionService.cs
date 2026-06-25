using DataLayer.EfCore;

namespace ServiceLayer.ConnectionService;

public class DatabaseConnectionService : IDatabaseConnection
{
    private string _connection = $"Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Default.swimdb")}";
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
