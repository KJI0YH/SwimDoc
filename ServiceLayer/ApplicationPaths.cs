namespace ServiceLayer;

public static class ApplicationPaths
{
    public const string AppFolderName = "SwimDoc";

    public static string InstallDirectory { get; } =
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string UserDataDirectory
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string GetUserDataFilePath(string fileName) =>
        Path.Combine(UserDataDirectory, fileName);

    public static string LogDirectory
    {
        get
        {
            var path = Path.Combine(UserDataDirectory, "logs");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string GetBundledFilePath(string fileName) =>
        Path.Combine(InstallDirectory, fileName);

    public static string EnsureUserDataFileFromBundle(string fileName)
    {
        var userPath = GetUserDataFilePath(fileName);
        if (File.Exists(userPath))
            return userPath;

        var bundledPath = GetBundledFilePath(fileName);
        if (File.Exists(bundledPath))
            File.Copy(bundledPath, userPath);

        return userPath;
    }
}
