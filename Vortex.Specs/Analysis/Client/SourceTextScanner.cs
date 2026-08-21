using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Vortex.Specs.Analysis.Client;

/// <summary>
/// Brace and comment handling shared by the ActionScript and TypeScript readers.
/// </summary>
/// <remarks>
/// Both trees are machine-generated or near-enough: the decompiler emits one statement per line with
/// fixed indentation, and Nitro's packet classes are formulaic. A brace-and-line scanner is therefore
/// reliable on them where it would not be on hand-written code — and where it is not, the readers
/// record what they could not follow instead of pretending the layout ended.
/// </remarks>
public static class SourceTextScanner
{
    /// <summary>Blanks out comments and string bodies so brace counting is not fooled by them.</summary>
    public static string Mask(string text)
    {
        char[] masked = text.ToCharArray();
        bool inLine = false;
        bool inBlock = false;
        char quote = '\0';

        for (int i = 0; i < masked.Length; i++)
        {
            char c = masked[i];
            char next = i + 1 < masked.Length ? masked[i + 1] : '\0';

            if (inLine)
            {
                if (c == '\n')
                {
                    inLine = false;
                }
                else
                {
                    masked[i] = ' ';
                }

                continue;
            }

            if (inBlock)
            {
                if (c == '*' && next == '/')
                {
                    masked[i] = ' ';
                    masked[i + 1] = ' ';
                    i++;
                    inBlock = false;
                }
                else if (c != '\n')
                {
                    masked[i] = ' ';
                }

                continue;
            }

            if (quote != '\0')
            {
                if (c == '\\')
                {
                    masked[i] = ' ';

                    if (i + 1 < masked.Length)
                    {
                        masked[i + 1] = ' ';
                        i++;
                    }

                    continue;
                }

                if (c == quote)
                {
                    quote = '\0';
                }
                else if (c != '\n')
                {
                    masked[i] = ' ';
                }

                continue;
            }

            switch (c)
            {
                case '/' when next == '/':
                    masked[i] = ' ';
                    masked[i + 1] = ' ';
                    i++;
                    inLine = true;
                    break;
                case '/' when next == '*':
                    masked[i] = ' ';
                    masked[i + 1] = ' ';
                    i++;
                    inBlock = true;
                    break;
                case '"' or '\'' or '`':
                    quote = c;
                    break;
            }
        }

        return new string(masked);
    }

    /// <summary>
    /// The body of the block that opens at the first <c>{</c> at or after <paramref name="from"/>,
    /// as an index range into the original text.
    /// </summary>
    public static (int Start, int End)? BlockAfter(string masked, int from)
    {
        int open = masked.IndexOf('{', from);

        if (open < 0)
        {
            return null;
        }

        int depth = 0;

        for (int i = open; i < masked.Length; i++)
        {
            if (masked[i] == '{')
            {
                depth++;
            }
            else if (masked[i] == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return (open + 1, i);
                }
            }
        }

        return null;
    }

    /// <summary>The 1-based line number of a character offset.</summary>
    public static int LineAt(string text, int offset)
    {
        int line = 1;

        for (int i = 0; i < offset && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    /// <summary>Every match of <paramref name="pattern"/> in source order.</summary>
    public static IEnumerable<Match> Matches(string masked, Regex pattern, int start, int end)
    {
        foreach (Match match in pattern.Matches(masked, start))
        {
            if (match.Index >= end)
            {
                yield break;
            }

            yield return match;
        }
    }

    /// <summary>
    /// How deeply nested inside braces an offset sits, relative to a block start. Used to tell a read
    /// inside a loop body from one at the top level of a parse method.
    /// </summary>
    public static int DepthAt(string masked, int blockStart, int offset)
    {
        int depth = 0;

        for (int i = blockStart; i < offset && i < masked.Length; i++)
        {
            if (masked[i] == '{')
            {
                depth++;
            }
            else if (masked[i] == '}')
            {
                depth--;
            }
        }

        return depth;
    }

    public static string Flatten(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
