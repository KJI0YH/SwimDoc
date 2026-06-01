using System.Reflection;

namespace UI;

/// <summary>Display version sourced from <see cref="Version.props"/> at build time (semver only, no git hash).</summary>
public static class AppVersionInformation
{
    public static string Display
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version is not null)
                return version.ToString(3) ?? "0.0.0";

            var informational = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (string.IsNullOrEmpty(informational))
                return "0.0.0";

            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }
    }
}
