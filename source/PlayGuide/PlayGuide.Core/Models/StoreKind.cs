namespace PlayGuide.Core.Models;

/// <summary>
/// Identifies the game store / launcher a <see cref="GameEntry"/> or
/// <see cref="LibrarySource"/> belongs to.
/// </summary>
/// <remarks>
/// Only <see cref="Steam"/> is implemented in v1. The remaining values are
/// reserved for future <c>ILibraryScanner</c> providers.
/// </remarks>
public enum StoreKind
{
    /// <summary>Valve Steam.</summary>
    Steam,

    /// <summary>Epic Games Store.</summary>
    Epic,

    /// <summary>GOG Galaxy.</summary>
    Gog,

    /// <summary>Ubisoft Connect.</summary>
    Ubisoft,

    /// <summary>Battle.net.</summary>
    BattleNet,
}
