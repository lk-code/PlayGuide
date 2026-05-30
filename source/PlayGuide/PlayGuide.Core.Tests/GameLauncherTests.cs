using FluentAssertions;
using PlayGuide.Core.Launch;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Tests;

[TestClass]
public class GameLauncherTests
{
    [TestMethod]
    public void ResolveLaunchTarget_SteamGame_UsesRunGameIdUri()
    {
        var game = new GameEntry("steam:440", StoreKind.Steam, "TF2", SteamAppId: "440");

        GameLauncher.ResolveLaunchTarget(game).Should().Be("steam://rungameid/440");
    }

    [TestMethod]
    public void ResolveLaunchTarget_NonSteam_UsesExecutablePath()
    {
        var game = new GameEntry("gog:1", StoreKind.Gog, "Witcher", ExecutablePath: "/games/witcher/run");

        GameLauncher.ResolveLaunchTarget(game).Should().Be("/games/witcher/run");
    }

    [TestMethod]
    public void ResolveLaunchTarget_NoTarget_ReturnsNull()
    {
        var game = new GameEntry("gog:2", StoreKind.Gog, "Unknown");

        GameLauncher.ResolveLaunchTarget(game).Should().BeNull();
    }
}
