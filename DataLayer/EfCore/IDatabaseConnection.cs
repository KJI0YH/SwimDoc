namespace DataLayer.EfCore;

public interface IDatabaseConnection
{
    string? CurrentConnection();
    public void SetConnection(string connectionString);
}