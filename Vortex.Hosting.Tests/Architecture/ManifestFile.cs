using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vortex.Hosting.Tests.Architecture;

/// <summary>
/// A reader for the flat `- key: value` YAML the workflow files use. The two files it parses are
/// written by hand and deliberately kept to one nesting level of lists, which is exactly what this
/// handles — adding a YAML dependency to read fifty lines of our own text would be the wrong trade.
/// Anything more structured than this belongs in a real parser, and that is the signal to reach for
/// one.
/// </summary>
internal static class ManifestFile
{
    /// <summary>
    /// Reads the entries of a list block: every `- key: value` starts a record and the indented
    /// `key: value` lines that follow join it. Block scalars (`>` / `|`) are skipped rather than
    /// folded — no guard asserts on prose.
    /// </summary>
    public static List<Dictionary<string, string>> ReadEntries(string path, string listKey)
    {
        List<Dictionary<string, string>> entries = [];
        Dictionary<string, string>? current = null;
        bool inList = false;
        int blockIndent = -1;

        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.TrimEnd();
            string trimmed = line.TrimStart();
            int indent = line.Length - trimmed.Length;

            if (blockIndent >= 0)
            {
                // Inside a folded/literal scalar: it ends at the first line indented no further than
                // the key that opened it.
                if (trimmed.Length > 0 && indent <= blockIndent)
                {
                    blockIndent = -1;
                }
                else
                {
                    continue;
                }
            }

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (!inList)
            {
                inList = trimmed.StartsWith($"{listKey}:", StringComparison.Ordinal);

                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                current = [];
                entries.Add(current);
                trimmed = trimmed[2..];
                indent += 2;
            }
            else if (current is null)
            {
                continue;
            }

            int separator = trimmed.IndexOf(':', StringComparison.Ordinal);

            if (separator <= 0)
            {
                continue;
            }

            string key = trimmed[..separator].Trim();
            string value = trimmed[(separator + 1)..].Trim();

            if (value is ">" or "|" or ">-" or "|-")
            {
                blockIndent = indent;
                value = string.Empty;
            }

            current![key] = value;
        }

        return entries;
    }

    /// <summary>Top-level `key: value` scalars, and the keys of top-level mappings.</summary>
    public static IReadOnlyCollection<string> ReadTopLevelKeys(string path) =>
        File.ReadAllLines(path)
            .Where(l => l.Length > 0 && !char.IsWhiteSpace(l[0]) && !l.StartsWith('#'))
            .Select(l => l.Split(':')[0].Trim())
            .Where(k => k.Length > 0)
            .ToArray();
}
