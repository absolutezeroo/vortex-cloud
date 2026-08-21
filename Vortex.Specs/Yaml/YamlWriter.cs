using System;
using System.Collections.Generic;
using System.Text;

namespace Vortex.Specs.Yaml;

/// <summary>
/// Emits the block-YAML subset the spec files use.
/// </summary>
/// <remarks>
/// Hand-written rather than delegated to a general YAML library for one reason: the output has to be
/// byte-stable. A generic serializer reorders keys, re-flows collections and re-quotes scalars
/// between versions, and every one of those turns an unchanged rescan into a diff. The subset is
/// small — block mappings, block sequences, single-line scalars — and <see cref="YamlReader"/> reads
/// exactly what this writes.
/// </remarks>
public static class YamlWriter
{
    private const string Indent = "  ";

    public static string Write(YamlNode root, string? header = null)
    {
        StringBuilder builder = new();

        if (!string.IsNullOrEmpty(header))
        {
            foreach (string line in header.Split('\n'))
            {
                builder.Append("# ").Append(line.TrimEnd('\r')).Append('\n');
            }

            builder.Append('\n');
        }

        WriteBlock(builder, root, depth: 0, firstLineInline: false);

        return builder.ToString();
    }

    /// <summary>
    /// Writes <paramref name="node"/> as a block. <paramref name="firstLineInline"/> means the
    /// caller has already put a prefix — a <c>"- "</c> — on the current line, so the block's first
    /// physical line must not be padded again.
    /// </summary>
    private static void WriteBlock(
        StringBuilder builder,
        YamlNode node,
        int depth,
        bool firstLineInline
    )
    {
        switch (node)
        {
            case YamlScalar scalar:
                if (!firstLineInline)
                {
                    Pad(builder, depth);
                }

                builder.Append(FormatScalar(scalar)).Append('\n');
                break;

            case YamlSequence sequence:
                WriteSequence(builder, sequence, depth, firstLineInline);
                break;

            case YamlMapping mapping:
                WriteMapping(builder, mapping, depth, firstLineInline);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported node type {node.GetType().Name}."
                );
        }
    }

    private static void WriteSequence(
        StringBuilder builder,
        YamlSequence sequence,
        int depth,
        bool firstLineInline
    )
    {
        if (sequence.Items.Count == 0)
        {
            if (!firstLineInline)
            {
                Pad(builder, depth);
            }

            builder.Append("[]\n");
            return;
        }

        for (int i = 0; i < sequence.Items.Count; i++)
        {
            if (!(firstLineInline && i == 0))
            {
                Pad(builder, depth);
            }

            builder.Append("- ");
            WriteBlock(builder, sequence.Items[i], depth + 1, firstLineInline: true);
        }
    }

    private static void WriteMapping(
        StringBuilder builder,
        YamlMapping mapping,
        int depth,
        bool firstLineInline
    )
    {
        if (mapping.Entries.Count == 0)
        {
            if (!firstLineInline)
            {
                Pad(builder, depth);
            }

            builder.Append("{}\n");
            return;
        }

        for (int i = 0; i < mapping.Entries.Count; i++)
        {
            if (!(firstLineInline && i == 0))
            {
                Pad(builder, depth);
            }

            KeyValuePair<string, YamlNode> entry = mapping.Entries[i];
            builder.Append(FormatKey(entry.Key)).Append(':');

            switch (entry.Value)
            {
                case YamlScalar scalar:
                    builder.Append(' ').Append(FormatScalar(scalar)).Append('\n');
                    break;

                case YamlSequence sequence when sequence.Items.Count == 0:
                    builder.Append(" []\n");
                    break;

                case YamlMapping nested when nested.Entries.Count == 0:
                    builder.Append(" {}\n");
                    break;

                // A block sequence under a key sits at the key's own indent. That is the
                // conventional layout, and it keeps deeply nested specs off the right margin.
                case YamlSequence sequence:
                    builder.Append('\n');
                    WriteSequence(builder, sequence, depth, firstLineInline: false);
                    break;

                default:
                    builder.Append('\n');
                    WriteBlock(builder, entry.Value, depth + 1, firstLineInline: false);
                    break;
            }
        }
    }

    private static void Pad(StringBuilder builder, int depth)
    {
        for (int i = 0; i < depth; i++)
        {
            builder.Append(Indent);
        }
    }

    private static string FormatKey(string key) => NeedsQuoting(key) ? Quote(key) : key;

    private static string FormatScalar(YamlScalar scalar)
    {
        if (scalar.Value is null)
        {
            return "null";
        }

        if (scalar.IsPlain)
        {
            return scalar.Value;
        }

        return NeedsQuoting(scalar.Value) ? Quote(scalar.Value) : scalar.Value;
    }

    /// <summary>
    /// Quotes anything a reader could take for a different type or a structural token. Erring
    /// towards quoting is safe; erring the other way silently turns the string "no" into false.
    /// </summary>
    private static bool NeedsQuoting(string value)
    {
        if (value.Length == 0)
        {
            return true;
        }

        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
        {
            return true;
        }

        foreach (char c in value)
        {
            if (c is ':' or '#' or '\n' or '\r' or '\t' or '"' or '\'' or '\\' or '\0')
            {
                return true;
            }
        }

        if (
            value[0]
            is '-'
                or '?'
                or '&'
                or '*'
                or '!'
                or '|'
                or '>'
                or '%'
                or '@'
                or '`'
                or '['
                or ']'
                or '{'
                or '}'
                or ','
        )
        {
            return true;
        }

        return LooksLikeNonString(value);
    }

    private static bool LooksLikeNonString(string value)
    {
        if (
            value
            is "true"
                or "false"
                or "null"
                or "~"
                or "yes"
                or "no"
                or "on"
                or "off"
                or "True"
                or "False"
                or "Null"
        )
        {
            return true;
        }

        foreach (char c in value)
        {
            if (!char.IsAsciiDigit(c) && c is not ('.' or '+' or '-' or 'e' or 'E'))
            {
                return false;
            }
        }

        return true;
    }

    private static string Quote(string value)
    {
        StringBuilder builder = new(value.Length + 2);
        builder.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }
}
