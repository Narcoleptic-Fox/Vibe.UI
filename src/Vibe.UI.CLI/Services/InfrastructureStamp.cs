using Vibe.UI.CLI.Infrastructure;
using Vibe.UI.CLI.Models;

namespace Vibe.UI.CLI.Services;

public enum InfrastructureState
{
    /// <summary>No stamp, or one that is not a version: the CLI cannot tell.</summary>
    Unknown,

    /// <summary>Written by this version of the CLI.</summary>
    Current,

    /// <summary>Written by an older CLI; <c>vibe update --infrastructure</c> refreshes it.</summary>
    Older,

    /// <summary>Written by a newer CLI than the one running.</summary>
    Newer,
}

/// <summary>
/// Records in vibe.json which CLI version last wrote the project's infrastructure, and compares
/// it with the running CLI. A version is all it knows: two builds that report the same version
/// (every build from source reports the project's version) look identical to it, so a file
/// changed between them is not seen. Telling edited and outdated files apart needs a record per
/// file, which this is not.
/// </summary>
public static class InfrastructureStamp
{
    public static string CurrentVersion => CliVersion.Current;

    public static void Write(VibeConfig config) => config.InfrastructureVersion = CurrentVersion;

    public static InfrastructureState Check(VibeConfig config) =>
        Compare(config.InfrastructureVersion, CurrentVersion);

    public static InfrastructureState Compare(string? projectVersion, string cliVersion)
    {
        if (string.IsNullOrWhiteSpace(projectVersion))
        {
            return InfrastructureState.Unknown;
        }

        var order = SemanticVersion.Compare(projectVersion, cliVersion);
        return order switch
        {
            null => InfrastructureState.Unknown,
            < 0 => InfrastructureState.Older,
            > 0 => InfrastructureState.Newer,
            _ => InfrastructureState.Current,
        };
    }

    /// <summary>The line <c>vibe update</c> prints, or null when there is nothing to say.</summary>
    public static string? Message(VibeConfig config) =>
        Check(config) switch
        {
            InfrastructureState.Unknown =>
                $"This project's Vibe/ infrastructure has no version stamp, so this CLI ({CurrentVersion}) cannot tell whether it is current. "
                    + "vibe update --infrastructure refreshes it and records one.",
            InfrastructureState.Older =>
                $"This project's Vibe/ infrastructure was written by CLI {config.InfrastructureVersion}; this CLI is {CurrentVersion}. "
                    + "Run vibe update --infrastructure to refresh it.",
            InfrastructureState.Newer =>
                $"This project's Vibe/ infrastructure was written by CLI {config.InfrastructureVersion}, newer than this CLI ({CurrentVersion}). "
                    + "Components from this CLI may not match it; update the CLI.",
            _ => null,
        };
}

/// <summary>Semantic Versioning 2.0 precedence: 1.0.0-beta &lt; 1.0.0-beta.2 &lt; 1.0.0-beta.10 &lt; 1.0.0.</summary>
public static class SemanticVersion
{
    /// <summary>Negative, zero or positive like <see cref="IComparer{T}"/>; null if either is not a version.</summary>
    public static int? Compare(string left, string right)
    {
        if (!TryParse(left, out var a) || !TryParse(right, out var b))
        {
            return null;
        }

        for (var index = 0; index < 3; index++)
        {
            var core = a.Core[index].CompareTo(b.Core[index]);
            if (core != 0)
            {
                return core;
            }
        }

        // A version without a prerelease ranks above the same version with one.
        if (a.Prerelease.Length == 0 && b.Prerelease.Length == 0)
        {
            return 0;
        }

        if (a.Prerelease.Length == 0)
        {
            return 1;
        }

        if (b.Prerelease.Length == 0)
        {
            return -1;
        }

        for (var index = 0; index < Math.Min(a.Prerelease.Length, b.Prerelease.Length); index++)
        {
            var x = a.Prerelease[index];
            var y = b.Prerelease[index];
            var xNumeric = ulong.TryParse(x, out var xNumber);
            var yNumeric = ulong.TryParse(y, out var yNumber);

            var order = (xNumeric, yNumeric) switch
            {
                (true, true) => xNumber.CompareTo(yNumber),
                // Numeric identifiers rank below alphanumeric ones.
                (true, false) => -1,
                (false, true) => 1,
                _ => string.CompareOrdinal(x, y),
            };
            if (order != 0)
            {
                return Math.Sign(order);
            }
        }

        return Math.Sign(a.Prerelease.Length.CompareTo(b.Prerelease.Length));
    }

    private static bool TryParse(string text, out (ulong[] Core, string[] Prerelease) version)
    {
        version = default;
        var withoutBuild = text.Trim().Split('+', 2)[0];
        var parts = withoutBuild.Split('-', 2);
        var core = parts[0].Split('.');
        if (core.Length != 3)
        {
            return false;
        }

        var numbers = new ulong[3];
        for (var index = 0; index < 3; index++)
        {
            if (!ulong.TryParse(core[index], out numbers[index]))
            {
                return false;
            }
        }

        var prerelease = parts.Length == 2 ? parts[1].Split('.') : [];
        if (prerelease.Any(string.IsNullOrEmpty))
        {
            return false;
        }

        version = (numbers, prerelease);
        return true;
    }
}
