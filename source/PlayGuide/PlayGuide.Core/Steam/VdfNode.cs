namespace PlayGuide.Core.Steam;

/// <summary>
/// A node in a parsed Valve Data File (VDF/KeyValues) tree. A node is either a
/// leaf carrying a string <see cref="Value"/> or a container of named
/// <see cref="Children"/>.
/// </summary>
public sealed class VdfNode
{
    private readonly Dictionary<string, VdfNode> _children =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The string value for a leaf node; <c>null</c> for containers.</summary>
    public string? Value { get; internal set; }

    /// <summary>Child nodes keyed by name (case-insensitive).</summary>
    public IReadOnlyDictionary<string, VdfNode> Children => _children;

    internal void Add(string key, VdfNode node) => _children[key] = node;

    /// <summary>
    /// Gets the child node with the given key, or <c>null</c> when absent.
    /// </summary>
    public VdfNode? this[string key] =>
        _children.TryGetValue(key, out var node) ? node : null;

    /// <summary>
    /// Gets the string value of the child with the given key, or <c>null</c>.
    /// </summary>
    public string? GetString(string key) => this[key]?.Value;
}
