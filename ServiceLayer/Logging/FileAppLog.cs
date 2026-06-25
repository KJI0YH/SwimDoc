using System.Reflection;
using System.Text;

namespace ServiceLayer.Logging;

public sealed class FileAppLog : IAppLog, IDisposable
{
    private readonly object _writeLock = new();
    private readonly string _logFilePath;
    private bool _sessionHeaderWritten;

    public FileAppLog()
    {
        var logDirectory = Path.Combine(ApplicationPaths.UserDataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, $"swimdoc-{DateTime.Now:yyyy-MM-dd}.log");
    }

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        if (exception is null)
        {
            Write("ERROR", message);
            return;
        }

        Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    }

    public void Dispose()
    {
    }

    private void Write(string level, string message)
    {
        try
        {
            lock (_writeLock)
            {
                EnsureSessionHeader();
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, line, Encoding.UTF8);
            }
        }
        catch
        {
        }
    }

    private void EnsureSessionHeader()
    {
        if (_sessionHeaderWritten)
            return;

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";
        var header =
            $"{Environment.NewLine}--- SwimDoc session started {DateTime.Now:yyyy-MM-dd HH:mm:ss}, version {version} ---{Environment.NewLine}";
        File.AppendAllText(_logFilePath, header, Encoding.UTF8);
        _sessionHeaderWritten = true;
    }
}
