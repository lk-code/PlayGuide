using System.Collections.Immutable;
using System.Text.Json.Serialization;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Cache;

/// <summary>
/// Source-generated <see cref="System.Text.Json"/> context for PlayGuide's persisted
/// types, enabling fast, trim/AOT-friendly (de)serialization.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(GameCache))]
[JsonSerializable(typeof(ImmutableList<LibrarySource>))]
public partial class PlayGuideJsonContext : JsonSerializerContext
{
}
