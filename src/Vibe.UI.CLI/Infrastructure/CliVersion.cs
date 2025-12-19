using System.Reflection;

namespace Vibe.UI.CLI.Infrastructure;

internal static class CliVersion
{
    public static string Current => GetSemVer();

    private static string GetSemVer()
    {
        var informational = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational.Split('+', 2)[0];
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        return "0.0.0";
    }
}

