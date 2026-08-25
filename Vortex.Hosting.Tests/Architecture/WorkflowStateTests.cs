using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Vortex.Hosting.Tests.Architecture;

/// <summary>
/// <c>STATE.yaml</c> is what lets a session skip work: an audit still marked <c>valid</c> means its
/// watched paths have not moved and nothing needs re-reading. That is only safe while the file
/// itself is well formed — a mistyped path silently matches nothing, and an audit that watches
/// nothing is valid forever.
/// </summary>
public sealed class WorkflowStateTests
{
    private static readonly string[] REQUIRED_KEYS =
    [
        "workflow_version",
        "baseline",
        "references_verified",
        "phase",
        "active_slices",
        "accepted_decisions",
        "open_questions",
        "audits",
        "forbidden",
    ];

    [Fact]
    public void State_HasEveryRequiredKey()
    {
        IReadOnlyCollection<string> keys = ManifestFile.ReadTopLevelKeys(StatePath());

        REQUIRED_KEYS
            .Except(keys)
            .Should()
            .BeEmpty("a session reads these to decide what it may skip");
    }

    [Fact]
    public void EveryWatchedPath_MatchesSomethingInTheRepository()
    {
        string root = RepositoryPaths.Root();
        List<string> missing = [];

        foreach (string watched in WatchedPaths())
        {
            // The globs are all `<dir>/**` or a literal file; both reduce to "does this prefix
            // exist", which is the mistake worth catching (a renamed project, a typo).
            string prefix = watched
                .Replace("/**", string.Empty)
                .Replace('/', Path.DirectorySeparatorChar);
            string candidate = Path.Combine(root, prefix);

            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                missing.Add(watched);
            }
        }

        missing
            .Should()
            .BeEmpty(
                "a watched path that matches nothing makes its audit permanently and falsely valid"
            );
    }

    [Fact]
    public void EveryAcceptedDecision_HasAnAdrFile()
    {
        string decisions = RepositoryPaths.ArchitectureV4("decisions");

        foreach (string id in ListValues("accepted_decisions"))
        {
            Directory
                .EnumerateFiles(decisions, $"{id}-*.md")
                .Should()
                .NotBeEmpty("{0} is accepted but has no ADR to read", id);
        }
    }

    private static string StatePath() => RepositoryPaths.ArchitectureV4("STATE.yaml");

    /// <summary>Every `- path` under an `audits:` entry's `watched_paths:` block.</summary>
    private static IEnumerable<string> WatchedPaths()
    {
        bool inWatched = false;

        foreach (string raw in File.ReadAllLines(StatePath()))
        {
            string trimmed = raw.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith("watched_paths:", StringComparison.Ordinal))
            {
                inWatched = true;

                continue;
            }

            if (!trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                inWatched = false;

                continue;
            }

            if (inWatched)
            {
                yield return trimmed[2..].Trim();
            }
        }
    }

    private static IEnumerable<string> ListValues(string key)
    {
        bool inList = false;

        foreach (string raw in File.ReadAllLines(StatePath()))
        {
            string line = raw.TrimEnd();
            string trimmed = line.TrimStart();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line.Length > 0 ? line[0] : ' '))
            {
                inList = trimmed.StartsWith($"{key}:", StringComparison.Ordinal);

                continue;
            }

            if (inList && trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                yield return trimmed[2..].Trim();
            }
        }
    }
}
