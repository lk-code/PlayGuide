using System.Collections.Immutable;
using FluentAssertions;
using PlayGuide.Core.Cache;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Tests;

[TestClass]
public class CacheAndSettingsTests
{
    [TestMethod]
    public async Task GameCache_RoundTrips()
    {
        using var temp = new TempDirectory();
        var service = new GameCacheService(new UserPaths(temp.Path));

        var games = ImmutableList.Create(
            new GameEntry("steam:440", StoreKind.Steam, "Team Fortress 2", SteamAppId: "440"),
            new GameEntry("steam:570", StoreKind.Steam, "Dota 2", SteamAppId: "570"));
        var cache = new GameCache(GameCache.CurrentVersion, DateTimeOffset.UnixEpoch, games);

        await service.SaveAsync(cache);
        var loaded = await service.LoadAsync();

        loaded.Version.Should().Be(GameCache.CurrentVersion);
        loaded.Games.Should().BeEquivalentTo(games);
    }

    [TestMethod]
    public async Task GameCache_LoadReturnsEmptyWhenMissing()
    {
        using var temp = new TempDirectory();
        var service = new GameCacheService(new UserPaths(temp.Path));

        var loaded = await service.LoadAsync();

        loaded.Should().BeSameAs(GameCache.Empty);
    }

    [TestMethod]
    public async Task Settings_RoundTrips()
    {
        using var temp = new TempDirectory();
        var store = new JsonSettingsStore(new UserPaths(temp.Path));

        var sources = ImmutableList.Create(
            new LibrarySource(StoreKind.Steam, "/games/steam"),
            new LibrarySource(StoreKind.Epic, Enabled: false));

        await store.SaveSourcesAsync(sources);
        var loaded = await store.LoadSourcesAsync();

        loaded.Should().BeEquivalentTo(sources);
    }

    [TestMethod]
    public async Task Settings_LoadReturnsSteamDefaultWhenMissing()
    {
        using var temp = new TempDirectory();
        var store = new JsonSettingsStore(new UserPaths(temp.Path));

        var loaded = await store.LoadSourcesAsync();

        loaded.Should().ContainSingle().Which.Store.Should().Be(StoreKind.Steam);
    }
}
