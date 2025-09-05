using PlayGuide.Core.Models;

namespace PlayGuide.Core.Contracts;

/// <summary>
/// defines a provider that can retrieve installed games on the system
/// </summary>
public interface IGamesProvider
{
    /// <summary>
    /// returns a list of installed games on the system
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<Game>> GetInstalledGamesAsync();
}
