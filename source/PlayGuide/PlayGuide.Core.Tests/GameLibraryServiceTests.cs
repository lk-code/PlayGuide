using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PlayGuide.Core.Contracts;
using PlayGuide.Core.Library;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Tests;

[TestClass]
public class GameLibraryServiceTests
{
    private static GameLibraryService Create(
        IEnumerable<LibrarySource> sources,
        IEnumerable<ILibraryScanner> scanners,
        IEnumerable<IArtworkResolver> resolvers,
        IGameCacheService cache)
    {
        var settings = new Mock<ISettingsStore>();
        settings.Setup(s => s.LoadSourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources.ToImmutableList());

        return new GameLibraryService(
            settings.Object, scanners, resolvers, cache, NullLogger<GameLibraryService>.Instance);
    }

    [TestMethod]
    public async Task RefreshAsync_MergesResolvesArtworkSortsAndSaves()
    {
        var steamSource = new LibrarySource(StoreKind.Steam);
        var found = new[]
        {
            new GameEntry("steam:2", StoreKind.Steam, "Zork", SteamAppId: "2"),
            new GameEntry("steam:1", StoreKind.Steam, "Alpha", SteamAppId: "1"),
        };

        var scanner = new Mock<ILibraryScanner>();
        scanner.SetupGet(s => s.Store).Returns(StoreKind.Steam);
        scanner.Setup(s => s.ScanAsync(steamSource, It.IsAny<CancellationToken>()))
            .ReturnsAsync(found);

        var resolver = new Mock<IArtworkResolver>();
        resolver.SetupGet(r => r.Store).Returns(StoreKind.Steam);
        resolver.Setup(r => r.ResolveArtworkPath(It.IsAny<GameEntry>(), steamSource))
            .Returns<GameEntry, LibrarySource>((g, _) => $"/art/{g.SteamAppId}.jpg");

        var cache = new Mock<IGameCacheService>();
        GameCache? saved = null;
        cache.Setup(c => c.SaveAsync(It.IsAny<GameCache>(), It.IsAny<CancellationToken>()))
            .Callback<GameCache, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);

        var service = Create([steamSource], [scanner.Object], [resolver.Object], cache.Object);
        var result = await service.RefreshAsync();

        result.Select(g => g.Name).Should().ContainInOrder("Alpha", "Zork");
        result.Should().OnlyContain(g => g.ArtworkPath != null);
        saved.Should().NotBeNull();
        saved!.Games.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task RefreshAsync_SkipsDisabledSources()
    {
        var disabled = new LibrarySource(StoreKind.Steam, Enabled: false);
        var scanner = new Mock<ILibraryScanner>(MockBehavior.Strict);
        scanner.SetupGet(s => s.Store).Returns(StoreKind.Steam);

        var cache = new Mock<IGameCacheService>();
        cache.Setup(c => c.SaveAsync(It.IsAny<GameCache>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = Create([disabled], [scanner.Object], [], cache.Object);
        var result = await service.RefreshAsync();

        result.Should().BeEmpty();
        scanner.Verify(s => s.ScanAsync(It.IsAny<LibrarySource>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task RefreshAsync_ContinuesWhenAScannerThrows()
    {
        var source = new LibrarySource(StoreKind.Steam);
        var scanner = new Mock<ILibraryScanner>();
        scanner.SetupGet(s => s.Store).Returns(StoreKind.Steam);
        scanner.Setup(s => s.ScanAsync(source, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("boom"));

        var cache = new Mock<IGameCacheService>();
        cache.Setup(c => c.SaveAsync(It.IsAny<GameCache>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = Create([source], [scanner.Object], [], cache.Object);
        var result = await service.RefreshAsync();

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetCachedGamesAsync_ReturnsCachedGames()
    {
        var games = ImmutableList.Create(new GameEntry("steam:1", StoreKind.Steam, "Alpha"));
        var cache = new Mock<IGameCacheService>();
        cache.Setup(c => c.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameCache(GameCache.CurrentVersion, DateTimeOffset.UtcNow, games));

        var service = Create([], [], [], cache.Object);
        var result = await service.GetCachedGamesAsync();

        result.Should().BeEquivalentTo(games);
    }
}
