namespace DataLayer.EfCore;

public interface IDatabaseConnection
{
    string? CurrentConnection();
    void SetConnection(string connectionString);
    event Action<string>? ConnectionChanged;
}
