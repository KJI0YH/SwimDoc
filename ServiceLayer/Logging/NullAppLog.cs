namespace ServiceLayer.Logging;

public sealed class NullAppLog : IAppLog
{
    public static readonly NullAppLog Instance = new();

    public void Info(string message)
    {
    }

    public void Warning(string message)
    {
    }

    public void Error(string message, Exception? exception = null)
    {
    }
}
