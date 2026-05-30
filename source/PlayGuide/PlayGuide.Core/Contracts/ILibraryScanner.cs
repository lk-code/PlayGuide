using PlayGuide.Core.Models;

namespace PlayGuide.Core.Contracts;

/// <summary>
/// Discovers installed games for a single <see cref="StoreKind"/>. Implementations
/// are registered per store, allowing new launchers to be added without changing
/// callers.
/// </summary>
public interface ILibraryScanner
{
    /// <summary>The store this scanner handles.</summary>
    StoreKind Store { get; }

    /// <summary>
    /// Scans the given source for installed games.
    /// </summary>
    /// <param name="source">The configured library source (may carry a custom path).</param>
    /// <param name="cancellationToken">Token to cancel the scan.</param>
    /// <returns>The discovered games; empty when the store is not installed.</returns>
    Task<IReadOnlyList<GameEntry>> ScanAsync(LibrarySource source, CancellationToken cancellationToken = default);
}
