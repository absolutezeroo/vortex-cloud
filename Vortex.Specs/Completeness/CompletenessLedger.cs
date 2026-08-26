using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vortex.Specs.Yaml;

namespace Vortex.Specs.Completeness;

/// <summary>An obligation excluded from scoring, and the paper trail that allows the exclusion.</summary>
public sealed record DecisionRecord
{
    public required string Reason { get; init; }

    public required IReadOnlyList<string> Evidence { get; init; }

    public required string DecidedBy { get; init; }
}

/// <summary>Evidence that an implemented obligation was actually checked against the protocol.</summary>
public sealed record VerificationRecord
{
    public required string VerifiedAtCommit { get; init; }

    public required IReadOnlyList<string> Tests { get; init; }

    public required IReadOnlyList<string> Protocol { get; init; }
}

/// <summary>
/// The two hand-maintained files that are allowed to move an obligation's status, and the rules that
/// stop them being used to move it anywhere convenient.
/// </summary>
/// <remarks>
/// Both files are the obvious place to cheat: one can mark anything out of scope, the other can mark
/// anything done. So neither is trusted on shape alone — an entry missing a field it is required to
/// carry is a problem that fails the command, not an entry that is quietly ignored. Ignoring it
/// would leave a half-written record sitting in the tree looking authoritative.
/// </remarks>
public sealed class CompletenessLedger
{
    private CompletenessLedger(
        IReadOnlyDictionary<string, DecisionRecord> decisions,
        IReadOnlyDictionary<string, VerificationRecord> verifications,
        IReadOnlyList<string> problems
    )
    {
        Decisions = decisions;
        Verifications = verifications;
        Problems = problems;
    }

    public IReadOnlyDictionary<string, DecisionRecord> Decisions { get; }

    public IReadOnlyDictionary<string, VerificationRecord> Verifications { get; }

    /// <summary>Malformed entries, phrased so the reader knows which file and key to fix.</summary>
    public IReadOnlyList<string> Problems { get; }

    public static CompletenessLedger Empty { get; } =
        new(
            new Dictionary<string, DecisionRecord>(StringComparer.Ordinal),
            new Dictionary<string, VerificationRecord>(StringComparer.Ordinal),
            []
        );

    /// <summary>Reads <c>decisions.yaml</c> and <c>verification.yaml</c> from a completeness root.</summary>
    public static CompletenessLedger Load(string completenessRoot) =>
        FromText(
            ReadIfPresent(Path.Combine(completenessRoot, "decisions.yaml")),
            ReadIfPresent(Path.Combine(completenessRoot, "verification.yaml"))
        );

    public static CompletenessLedger FromText(string? decisions, string? verification)
    {
        List<string> problems = [];
        Dictionary<string, DecisionRecord> decided = new(StringComparer.Ordinal);
        Dictionary<string, VerificationRecord> verified = new(StringComparer.Ordinal);

        foreach (
            (string key, YamlMapping body) in Obligations(decisions, "decisions.yaml", problems)
        )
        {
            // Every field is required by decisions.yaml's own rules block. An exclusion nobody
            // signed is an exclusion nobody can argue with later.
            string? reason = body.String("reason");
            string? decidedBy = body.String("decided_by");
            List<string> evidence = Strings(body.SequenceAt("evidence"));

            List<string> missing = [];

            if (string.IsNullOrWhiteSpace(reason))
            {
                missing.Add("reason");
            }

            if (evidence.Count == 0)
            {
                missing.Add("evidence");
            }

            if (string.IsNullOrWhiteSpace(decidedBy))
            {
                missing.Add("decided_by");
            }

            if (missing.Count > 0)
            {
                problems.Add(
                    $"decisions.yaml: {key} claims not_applicable without {string.Join(", ", missing)}"
                );
                continue;
            }

            string reachability = body.String("reachability") ?? "not_applicable";

            if (!string.Equals(reachability, "not_applicable", StringComparison.Ordinal))
            {
                problems.Add(
                    $"decisions.yaml: {key} has reachability '{reachability}'; the only decision this "
                        + "file may record is not_applicable"
                );
                continue;
            }

            decided[key] = new DecisionRecord
            {
                Reason = reason!.Trim(),
                Evidence = evidence,
                DecidedBy = decidedBy!,
            };
        }

        foreach (
            (string key, YamlMapping body) in Obligations(
                verification,
                "verification.yaml",
                problems
            )
        )
        {
            string? commit = body.String("verified_at_commit");
            List<string> tests = Strings(body.SequenceAt("tests"));
            List<string> protocol = Strings(body.SequenceAt("protocol"));

            List<string> missing = [];

            if (string.IsNullOrWhiteSpace(commit))
            {
                missing.Add("verified_at_commit");
            }

            if (tests.Count == 0)
            {
                missing.Add("tests");
            }

            if (protocol.Count == 0)
            {
                missing.Add("protocol");
            }

            if (missing.Count > 0)
            {
                problems.Add(
                    $"verification.yaml: {key} cannot promote anything without {string.Join(", ", missing)}"
                );
                continue;
            }

            verified[key] = new VerificationRecord
            {
                VerifiedAtCommit = commit!,
                Tests = tests,
                Protocol = protocol,
            };
        }

        return new CompletenessLedger(
            decided,
            verified,
            [.. problems.OrderBy(p => p, StringComparer.Ordinal)]
        );
    }

    private static IEnumerable<(string Key, YamlMapping Body)> Obligations(
        string? text,
        string file,
        List<string> problems
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        YamlMapping root;

        try
        {
            root = YamlReader.ReadMapping(text);
        }
        catch (YamlParseException error)
        {
            // A hand edit that does not parse must not take the whole command down: the report is
            // still worth printing, with the broken file named.
            problems.Add($"{file}: {error.Message}");
            yield break;
        }

        if (root.Mapping("obligations") is not { } obligations)
        {
            yield break;
        }

        foreach (KeyValuePair<string, YamlNode> entry in obligations.Entries)
        {
            if (entry.Value is YamlMapping body)
            {
                yield return (entry.Key, body);
                continue;
            }

            problems.Add($"{file}: {entry.Key} is not a mapping");
        }
    }

    private static List<string> Strings(YamlSequence? sequence) =>
        sequence is null
            ? []
            :
            [
                .. sequence
                    .Items.OfType<YamlScalar>()
                    .Select(s => s.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v!),
            ];

    private static string? ReadIfPresent(string path) =>
        File.Exists(path) ? File.ReadAllText(path) : null;
}
