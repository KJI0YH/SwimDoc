using DataLayer.EfCore;

namespace ServiceLayer.ConnectionService;

public class DatabaseConnectionService : IDatabaseConnection
{
    private string _connection = $"Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Default.db")}";

    public string? CurrentConnection()
    {
        return _connection;
    }

    public void SetConnection(string connectionString)
    {
        _connection = connectionString;
    }
}