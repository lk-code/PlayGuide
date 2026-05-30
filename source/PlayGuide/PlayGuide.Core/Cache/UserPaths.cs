using System.Runtime.InteropServices;

namespace PlayGuide.Core.Cache;

/// <summary>
/// Resolves the per-user application data locations used by PlayGuide, following
/// each platform's conventions.
/// </summary>
public sealed class UserPaths
{
    private const string AppFolderName = "PlayGuide";

    /// <summary>
    /// Creates a <see cref="UserPaths"/> rooted at the platform-appropriate
    /// per-user application data directory.
    /// </summary>
    public UserPaths()
        : this(GetDefaultRoot())
    {
    }

    /// <summary>
    /// Creates a <see cref="UserPaths"/> rooted at an explicit directory. Intended
    /// for tests that need an isolated, temporary location.
    /// </summary>
    /// <param name="rootDirectory">The PlayGuide data directory to use.</param>
    public UserPaths(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        RootDirectory = rootDirectory;
    }

    /// <summary>The PlayGuide application-data directory.</summary>
    public string RootDirectory { get; }

    /// <summary>Path to the configured-sources settings file.</summary>
    public string SettingsFile => Path.Combine(RootDirectory, "settings.json");

    /// <summary>Path to the discovered-games cache file.</summary>
    public string GameCacheFile => Path.Combine(RootDirectory, "games-cache.json");

    /// <summary>Directory for any locally copied artwork.</summary>
    public string ArtworkDirectory => Path.Combine(RootDirectory, "artwork");

    /// <summary>Ensures the root directory exists, creating it when necessary.</summary>
    public void EnsureCreated() => Directory.CreateDirectory(RootDirectory);

    private static string GetDefaultRoot()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppFolderName);
        }

        // Windows -> %APPDATA%; Linux -> $XDG_CONFIG_HOME or ~/.config (via .NET).
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create);
        return Path.Combine(appData, AppFolderName);
    }
}
