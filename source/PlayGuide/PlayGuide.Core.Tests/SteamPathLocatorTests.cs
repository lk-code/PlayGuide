using FluentAssertions;
using PlayGuide.Core.Steam;

namespace PlayGuide.Core.Tests;

[TestClass]
public class SteamPathLocatorTests
{
    [TestMethod]
    public void ResolveRoot_HonoursCustomPathWhenItHasSteamApps()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Combine("steamapps"));
        var locator = new SteamPathLocator();

        locator.ResolveRoot(temp.Path).Should().Be(temp.Path);
    }

    [TestMethod]
    public void ResolveRoot_IgnoresCustomPathWithoutSteamApps()
    {
        using var temp = new TempDirectory();
        var locator = new SteamPathLocator();

        // No steamapps folder -> not a valid root; falls back to (likely absent) defaults.
        locator.ResolveRoot(temp.Path).Should().NotBe(temp.Path);
    }

    [TestMethod]
    public void GetLibraryFolders_IncludesRootAndAdditionalLibraries()
    {
        using var temp = new TempDirectory();
        var primary = temp.Combine("Steam");
        var extra = temp.Combine("ExtraLibrary");
        Directory.CreateDirectory(Path.Combine(primary, "steamapps"));
        Directory.CreateDirectory(Path.Combine(extra, "steamapps"));

        var escapedExtra = extra.Replace("\\", "\\\\");
        File.WriteAllText(Path.Combine(primary, "steamapps", "libraryfolders.vdf"), $$"""
            "libraryfolders"
            {
                "0"
                {
                    "path" "{{primary.Replace("\\", "\\\\")}}"
                }
                "1"
                {
                    "path" "{{escapedExtra}}"
                }
            }
            """);

        var locator = new SteamPathLocator();
        var folders = locator.GetLibraryFolders(primary);

        folders.Should().Contain(primary);
        folders.Should().Contain(extra);
    }

    [TestMethod]
    public void GetLibraryFolders_SkipsLibrariesWithoutSteamApps()
    {
        using var temp = new TempDirectory();
        var primary = temp.Combine("Steam");
        var missing = temp.Combine("Missing");
        Directory.CreateDirectory(Path.Combine(primary, "steamapps"));

        File.WriteAllText(Path.Combine(primary, "steamapps", "libraryfolders.vdf"), $$"""
            "libraryfolders"
            {
                "0" { "path" "{{missing.Replace("\\", "\\\\")}}" }
            }
            """);

        var folders = new SteamPathLocator().GetLibraryFolders(primary);

        folders.Should().ContainSingle().Which.Should().Be(primary);
    }
}
