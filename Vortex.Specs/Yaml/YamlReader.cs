using System;
using System.Collections.Generic;
using System.Text;

namespace Vortex.Specs.Yaml;

public sealed class YamlParseException(string message, int line)
    : Exception($"{message} (line {line})")
{
    public int Line { get; } = line;
}

/// <summary>
/// Reads the block-YAML subset <see cref="YamlWriter"/> emits, plus the flow forms a human is
/// likely to hand-write when editing a <c>verified:</c> block by hand.
/// </summary>
/// <remarks>
/// Anything outside the subset — anchors, aliases, multi-document streams, tags — throws with a line
/// number instead of being silently reinterpreted. A spec reader that quietly drops what it does not
/// understand would lose exactly the hand-written material this format exists to protect.
/// </remarks>
public static class YamlReader
{
    private sealed record Line(int Indent, string Content, int Number);

    public static YamlNode Read(string text)
    {
        List<Line> lines = Tokenize(text);

        if (lines.Count == 0)
        {
            return YamlNode.Mapping();
        }

        int cursor = 0;
        YamlNode node = ParseBlock(lines, ref cursor, lines[0].Indent);

        if (cursor < lines.Count)
        {
            throw new YamlParseException(
                "Unexpected content after the document body",
                lines[cursor].Number
            );
        }

        return node;
    }

    public static YamlMapping ReadMapping(string text) =>
        Read(text) as YamlMapping
        ?? throw new YamlParseException("Expected a mapping at the document root", 1);

    private static List<Line> Tokenize(string text)
    {
        List<Line> lines = [];
        string[] raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int i = 0; i < raw.Length; i++)
        {
            string line = raw[i];
            int indent = 0;

            while (indent < line.Length && line[indent] == ' ')
            {
                indent++;
            }

            if (indent < line.Length && line[indent] == '\t')
            {
                throw new YamlParseException("Tabs cannot be used for indentation", i + 1);
            }

            string content = StripComment(line[indent..]).TrimEnd();

            if (content.Length == 0 || content == "---")
            {
                continue;
            }

            if (content[0] is '&' or '*' or '!')
            {
                throw new YamlParseException(
                    "Anchors, aliases and tags are outside the spec YAML subset",
                    i + 1
                );
            }

            lines.Add(new Line(indent, content, i + 1));
        }

        return lines;
    }

    private static string StripComment(string content)
    {
        bool inSingle = false;
        bool inDouble = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (c == '\\' && inDouble)
            {
                i++;
                continue;
            }

            if (c == '\'' && !inDouble)
            {
                inSingle = !inSingle;
            }
            else if (c == '"' && !inSingle)
            {
                inDouble = !inDouble;
            }
            else if (c == '#' && !inSingle && !inDouble && (i == 0 || content[i - 1] == ' '))
            {
                return content[..i];
            }
        }

        return content;
    }

    private static YamlNode ParseBlock(List<Line> lines, ref int cursor, int indent)
    {
        if (cursor >= lines.Count)
        {
            return YamlNode.Null();
        }

        string content = lines[cursor].Content;

        if (content.StartsWith("- ", StringComparison.Ordinal) || content == "-")
        {
            return ParseSequence(lines, ref cursor, indent);
        }

        if (LooksLikeMappingEntry(content))
        {
            return ParseMapping(lines, ref cursor, indent);
        }

        // A block that is neither a mapping entry nor a sequence item is a bare scalar — the body of
        // a plain sequence item, or a value a hand edit put on its own line.
        Line scalarLine = lines[cursor];
        cursor++;
        return ParseInlineValue(scalarLine.Content, scalarLine.Number);
    }

    /// <summary>
    /// True when the line opens a mapping entry. Flow collections are excluded explicitly: a
    /// <c>{ has_rights: false }</c> item carries a colon but is a value, not a key.
    /// </summary>
    private static bool LooksLikeMappingEntry(string content)
    {
        if (content.Length == 0 || content[0] is '[' or '{')
        {
            return false;
        }

        if (content[0] is '"' or '\'')
        {
            char quote = content[0];

            for (int i = 1; i < content.Length; i++)
            {
                if (content[i] == '\\' && quote == '"')
                {
                    i++;
                    continue;
                }

                if (content[i] == quote)
                {
                    return i + 1 < content.Length && content[i + 1] == ':';
                }
            }

            return false;
        }

        return IndexOfKeyColon(content) >= 0;
    }

    private static YamlSequence ParseSequence(List<Line> lines, ref int cursor, int indent)
    {
        YamlSequence sequence = YamlNode.Sequence();

        while (cursor < lines.Count && lines[cursor].Indent == indent)
        {
            Line line = lines[cursor];

            if (!line.Content.StartsWith('-'))
            {
                break;
            }

            string rest = line.Content.Length > 1 ? line.Content[1..].TrimStart() : string.Empty;
            int restColumn = indent + (line.Content.Length - rest.Length);

            if (rest.Length == 0)
            {
                cursor++;
                sequence.Add(
                    cursor < lines.Count && lines[cursor].Indent > indent
                        ? ParseBlock(lines, ref cursor, lines[cursor].Indent)
                        : YamlNode.Null()
                );
                continue;
            }

            // Re-present the text after the dash as a line of its own at the column it actually
            // occupies. Sequence items whose body starts on the dash line then parse with the same
            // code as ones that start on the next line, instead of a second near-copy of it.
            lines[cursor] = new Line(restColumn, rest, line.Number);
            sequence.Add(ParseBlock(lines, ref cursor, restColumn));
        }

        return sequence;
    }

    private static YamlMapping ParseMapping(List<Line> lines, ref int cursor, int indent)
    {
        YamlMapping mapping = YamlNode.Mapping();

        while (cursor < lines.Count && lines[cursor].Indent == indent)
        {
            Line line = lines[cursor];

            if (line.Content.StartsWith("- ", StringComparison.Ordinal))
            {
                break;
            }

            (string key, string rest) = SplitKey(line);
            cursor++;

            if (rest.Length > 0)
            {
                mapping.Set(key, ParseInlineValue(rest, line.Number));
                continue;
            }

            if (cursor >= lines.Count)
            {
                mapping.Set(key, YamlNode.Null());
                continue;
            }

            Line next = lines[cursor];

            if (next.Indent > indent)
            {
                mapping.Set(key, ParseBlock(lines, ref cursor, next.Indent));
                continue;
            }

            // A block sequence under a key is conventionally written at the key's own indent.
            if (next.Indent == indent && next.Content.StartsWith('-'))
            {
                mapping.Set(key, ParseSequence(lines, ref cursor, indent));
                continue;
            }

            mapping.Set(key, YamlNode.Null());
        }

        return mapping;
    }

    private static (string Key, string Value) SplitKey(Line line)
    {
        string content = line.Content;

        if (content[0] is '"' or '\'')
        {
            char quote = content[0];
            int end = FindClosingQuote(content, quote, line.Number);
            string quoted = Unescape(content[..(end + 1)], line.Number);

            if (end + 1 >= content.Length || content[end + 1] != ':')
            {
                throw new YamlParseException("Expected ':' after a quoted key", line.Number);
            }

            return (quoted, content[(end + 2)..].TrimStart());
        }

        int colon = IndexOfKeyColon(content);

        if (colon < 0)
        {
            throw new YamlParseException(
                $"Expected 'key: value' but found '{content}'",
                line.Number
            );
        }

        return (content[..colon].TrimEnd(), content[(colon + 1)..].TrimStart());
    }

    private static int IndexOfKeyColon(string content)
    {
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] != ':')
            {
                continue;
            }

            if (i + 1 == content.Length || content[i + 1] == ' ')
            {
                return i;
            }
        }

        return -1;
    }

    private static YamlNode ParseInlineValue(string text, int lineNumber)
    {
        if (text == "{}")
        {
            return YamlNode.Mapping();
        }

        if (text == "[]")
        {
            return YamlNode.Sequence();
        }

        if (text.StartsWith('[') && text.EndsWith(']'))
        {
            YamlSequence sequence = YamlNode.Sequence();

            foreach (string part in SplitFlow(text[1..^1], lineNumber))
            {
                sequence.Add(ParseInlineValue(part.Trim(), lineNumber));
            }

            return sequence;
        }

        if (text.StartsWith('{') && text.EndsWith('}'))
        {
            YamlMapping mapping = YamlNode.Mapping();

            foreach (string part in SplitFlow(text[1..^1], lineNumber))
            {
                int colon = part.IndexOf(':', StringComparison.Ordinal);

                if (colon < 0)
                {
                    throw new YamlParseException(
                        "Expected 'key: value' inside a flow mapping",
                        lineNumber
                    );
                }

                mapping.Set(
                    part[..colon].Trim().Trim('"', '\''),
                    ParseInlineValue(part[(colon + 1)..].Trim(), lineNumber)
                );
            }

            return mapping;
        }

        if (text is "|" or ">" or "|-" or ">-")
        {
            throw new YamlParseException(
                "Block scalars are outside the spec YAML subset; keep values on one line",
                lineNumber
            );
        }

        if (text is "null" or "~")
        {
            return YamlNode.Null();
        }

        if (text[0] is '"' or '\'')
        {
            return YamlNode.Scalar(Unescape(text, lineNumber));
        }

        return new YamlScalar(text) { IsPlain = true };
    }

    private static List<string> SplitFlow(string text, int lineNumber)
    {
        List<string> parts = [];
        StringBuilder current = new();
        int depth = 0;
        bool inSingle = false;
        bool inDouble = false;

        foreach (char c in text)
        {
            if (c == '\'' && !inDouble)
            {
                inSingle = !inSingle;
            }
            else if (c == '"' && !inSingle)
            {
                inDouble = !inDouble;
            }
            else if (!inSingle && !inDouble)
            {
                if (c is '[' or '{')
                {
                    depth++;
                }
                else if (c is ']' or '}')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    continue;
                }
            }

            current.Append(c);
        }

        if (inSingle || inDouble)
        {
            throw new YamlParseException(
                "Unterminated quoted scalar in a flow collection",
                lineNumber
            );
        }

        if (current.ToString().Trim().Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts;
    }

    private static int FindClosingQuote(string content, char quote, int lineNumber)
    {
        for (int i = 1; i < content.Length; i++)
        {
            if (content[i] == '\\' && quote == '"')
            {
                i++;
                continue;
            }

            if (content[i] == quote)
            {
                return i;
            }
        }

        throw new YamlParseException("Unterminated quoted scalar", lineNumber);
    }

    private static string Unescape(string text, int lineNumber)
    {
        char quote = text[0];
        int end = FindClosingQuote(text, quote, lineNumber);
        string body = text[1..end];

        if (quote == '\'')
        {
            return body.Replace("''", "'");
        }

        StringBuilder builder = new(body.Length);

        for (int i = 0; i < body.Length; i++)
        {
            if (body[i] != '\\' || i + 1 >= body.Length)
            {
                builder.Append(body[i]);
                continue;
            }

            i++;
            builder.Append(
                body[i] switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '"' => '"',
                    '\\' => '\\',
                    '0' => '\0',
                    _ => body[i],
                }
            );
        }

        return builder.ToString();
    }
}
