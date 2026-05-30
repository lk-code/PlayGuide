using System.Text;

namespace PlayGuide.Core.Steam;

/// <summary>
/// A minimal parser for Valve's text-format Data Files (KeyValues), used by Steam's
/// <c>appmanifest_*.acf</c> and <c>libraryfolders.vdf</c>.
/// </summary>
/// <remarks>
/// The format is a tree of quoted tokens: a key token followed either by a quoted
/// value token or by a <c>{ ... }</c> block of nested key/value pairs. Line comments
/// starting with <c>//</c> and Valve <c>[$CONDITIONAL]</c> tokens are ignored. This
/// parser intentionally covers only what Steam emits, avoiding an external dependency.
/// </remarks>
public static class VdfParser
{
    /// <summary>
    /// Parses VDF text into a <see cref="VdfNode"/> tree.
    /// </summary>
    /// <param name="text">The VDF document.</param>
    /// <returns>The root container node holding the document's top-level keys.</returns>
    /// <exception cref="FormatException">Thrown on unbalanced braces or a dangling key.</exception>
    public static VdfNode Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var root = new VdfNode();
        var index = 0;
        ParseInto(text, ref index, root, isRoot: true);
        return root;
    }

    private static void ParseInto(string text, ref int index, VdfNode container, bool isRoot)
    {
        while (true)
        {
            var key = ReadToken(text, ref index);
            if (key is null)
            {
                // End of input. At the root this is valid; inside a block it means
                // the closing brace was missing.
                if (!isRoot)
                {
                    throw new FormatException("Unexpected end of VDF input inside a block.");
                }
                return;
            }

            if (key == "}")
            {
                if (isRoot)
                {
                    throw new FormatException("Unexpected '}' at the root of the VDF document.");
                }
                return;
            }

            // After a key we expect either a value token or the start of a block.
            var next = ReadToken(text, ref index);
            if (next is null)
            {
                throw new FormatException($"Key '{key}' has no value or block.");
            }

            if (next == "{")
            {
                var child = new VdfNode();
                ParseInto(text, ref index, child, isRoot: false);
                container.Add(key, child);
            }
            else if (next == "}")
            {
                throw new FormatException($"Key '{key}' is missing its value.");
            }
            else
            {
                container.Add(key, new VdfNode { Value = next });
            }
        }
    }

    /// <summary>
    /// Reads the next significant token: a quoted string, a brace, or an unquoted
    /// run of non-whitespace characters. Returns <c>null</c> at end of input.
    /// </summary>
    private static string? ReadToken(string text, ref int index)
    {
        SkipTrivia(text, ref index);
        if (index >= text.Length)
        {
            return null;
        }

        var c = text[index];
        if (c is '{' or '}')
        {
            index++;
            return c.ToString();
        }

        if (c == '"')
        {
            return ReadQuoted(text, ref index);
        }

        // Unquoted token (rare in Steam files, but valid in KeyValues).
        var start = index;
        while (index < text.Length && !char.IsWhiteSpace(text[index]) &&
               text[index] is not ('{' or '}' or '"'))
        {
            index++;
        }
        return text[start..index];
    }

    private static string ReadQuoted(string text, ref int index)
    {
        // Caller guarantees text[index] == '"'.
        index++;
        var sb = new StringBuilder();
        while (index < text.Length)
        {
            var c = text[index++];
            if (c == '\\' && index < text.Length)
            {
                var escaped = text[index++];
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => escaped,
                });
            }
            else if (c == '"')
            {
                return sb.ToString();
            }
            else
            {
                sb.Append(c);
            }
        }
        throw new FormatException("Unterminated quoted string in VDF input.");
    }

    /// <summary>Skips whitespace, <c>//</c> line comments and <c>[$COND]</c> tokens.</summary>
    private static void SkipTrivia(string text, ref int index)
    {
        while (index < text.Length)
        {
            var c = text[index];
            if (char.IsWhiteSpace(c))
            {
                index++;
            }
            else if (c == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                while (index < text.Length && text[index] is not ('\n' or '\r'))
                {
                    index++;
                }
            }
            else if (c == '[')
            {
                // Platform conditional, e.g. [$WINDOWS]; ignore up to the closing ']'.
                while (index < text.Length && text[index] != ']')
                {
                    index++;
                }
                if (index < text.Length)
                {
                    index++; // consume ']'
                }
            }
            else
            {
                return;
            }
        }
    }
}
