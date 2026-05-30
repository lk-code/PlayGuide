using System.Runtime.InteropServices;

namespace PlayGuide.Core.Steam;

/// <summary>
/// Locates the Steam installation root and enumerates its Steam library folders
/// across Windows, Linux and macOS.
/// </summary>
public sealed class SteamPathLocator
{
    /// <summary>
    /// Returns the platform-specific candidate paths for the Steam root directory,
    /// most-likely first.
    /// </summary>
    public IReadOnlyList<string> GetDefaultRootCandidates()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var candidates = new List<string>();
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFilesX86))
            {
                candidates.Add(Path.Combine(programFilesX86, "Steam"));
            }
            if (!string.IsNullOrEmpty(programFiles))
            {
                candidates.Add(Path.Combine(programFiles, "Steam"));
            }
            // Note: the authoritative location lives in the registry value
            // HKCU\Software\Valve\Steam\SteamPath; these defaults cover the common case.
            return candidates;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return [Path.Combine(home, "Library", "Application Support", "Steam")];
        }

        // Linux (and other Unix): native, Flatpak and Snap locations.
        return
        [
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".steam", "root"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
            Path.Combine(home, "snap", "steam", "common", ".local", "share", "Steam"),
        ];
    }

    /// <summary>
    /// Resolves the Steam root directory, honouring an optional user override.
    /// </summary>
    /// <param name="customPath">Optional explicit Steam root from settings.</param>
    /// <returns>The first existing Steam root containing a <c>steamapps</c> folder, or <c>null</c>.</returns>
    public string? ResolveRoot(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath) && IsSteamRoot(customPath))
        {
            return customPath;
        }

        foreach (var candidate in GetDefaultRootCandidates())
        {
            if (IsSteamRoot(candidate))
            {
                return candidate;
            }
        }
        return null;
    }

    /// <summary>
    /// Enumerates all Steam library folders for a resolved root, including the root's
    /// own library. Each returned path is a library root that contains a
    /// <c>steamapps</c> sub-folder.
    /// </summary>
    public IReadOnlyList<string> GetLibraryFolders(string steamRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(steamRoot);

        var libraries = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string libraryRoot)
        {
            var steamApps = Path.Combine(libraryRoot, "steamapps");
            if (Directory.Exists(steamApps) && seen.Add(Path.GetFullPath(libraryRoot)))
            {
                libraries.Add(libraryRoot);
            }
        }

        TryAdd(steamRoot);

        var manifest = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (File.Exists(manifest))
        {
            try
            {
                var root = VdfParser.Parse(File.ReadAllText(manifest));
                // Layout is { "libraryfolders" { "0" { "path" "..." } "1" { ... } } }
                // or, on older clients, { "LibraryFolders" { "1" "path" ... } }.
                var folders = root["libraryfolders"] ?? root["LibraryFolders"];
                if (folders is not null)
                {
                    foreach (var (_, node) in folders.Children)
                    {
                        var path = node.Value ?? node.GetString("path");
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            TryAdd(path);
                        }
                    }
                }
            }
            catch (FormatException)
            {
                // A corrupt manifest should not prevent scanning the primary library.
            }
        }

        return libraries;
    }

    private static bool IsSteamRoot(string path) =>
        Directory.Exists(Path.Combine(path, "steamapps"));
}
