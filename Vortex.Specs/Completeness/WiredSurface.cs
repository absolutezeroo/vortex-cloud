using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Completeness;

/// <summary>
/// The six kinds of wired box, as both the client and this emulator group them.
/// </summary>
/// <remarks>
/// The families exist on both sides and the numbering restarts inside each, so a code alone is not
/// an identity — <c>0</c> is a trigger, an action, a condition and a variable at once.
/// </remarks>
public enum WiredBoxFamily
{
    Trigger,
    Action,
    Condition,
    Selector,
    Addon,
    Variable,
}

/// <summary>One configurable box the target client has a form for, and what Vortex does about it.</summary>
public sealed record WiredBoxObligation
{
    public required WiredBoxFamily Family { get; init; }

    public required int Code { get; init; }

    /// <summary>
    /// The client's constant name, or null when obfuscation took it. The code survives either way,
    /// and it is the code both sides route on.
    /// </summary>
    public string? ClientName { get; init; }

    /// <summary>The Vortex logic class bound to this code, when one is.</summary>
    public string? VortexLogic { get; init; }

    /// <summary>The Vortex enum member, which names a box the client's own constant no longer does.</summary>
    public string? VortexName { get; init; }

    public bool Implemented => VortexLogic is not null;

    /// <summary>Best available name: the client's, else Vortex's, else the bare code.</summary>
    public string Name =>
        ClientName ?? VortexName ?? $"{Family.ToString().ToLowerInvariant()}:{Code}";

    public string Id => $"wired/{Family.ToString().ToLowerInvariant()}/{Code}";
}

public sealed record WiredSurfaceReport
{
    public required string? TargetRevision { get; init; }

    public required IReadOnlyList<WiredBoxObligation> Boxes { get; init; }

    /// <summary>
    /// Codes this emulator implements that the target client has no form for. Not a gap — the
    /// opposite — but worth printing: a box nobody can configure is code nobody can reach.
    /// </summary>
    public required IReadOnlyList<WiredBoxObligation> UnreachableInVortex { get; init; }

    public required IReadOnlyList<string> Problems { get; init; }

    public bool HasTargetClient => TargetRevision is not null;

    public int Implemented => Boxes.Count(b => b.Implemented);

    public IEnumerable<IGrouping<WiredBoxFamily, WiredBoxObligation>> ByFamily =>
        Boxes.GroupBy(b => b.Family).OrderBy(g => g.Key);

    public string Share =>
        !HasTargetClient || Boxes.Count == 0
            ? "n/a"
            : string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} / {1} ({2:0.0}%)",
                Implemented,
                Boxes.Count,
                100.0 * Implemented / Boxes.Count
            );
}

/// <summary>
/// The wired box surface: what the target client can configure versus what this emulator binds.
/// </summary>
/// <remarks>
/// A second denominator, and a necessary one. The packet matrix reports the wired domain at 42/43
/// because it counts the six <c>Update*</c> messages that carry a box's configuration — and those
/// messages are equally well implemented whether the box they configure exists here or not. A player
/// dragging an unimplemented box out of the catalogue configures it, saves it, gets a success reply,
/// and it never fires.
/// </remarks>
public static partial class WiredSurfaceAnalyzer
{
    private static readonly Dictionary<string, WiredBoxFamily> ClientFiles = new(
        StringComparer.Ordinal
    )
    {
        ["triggerconfs/TriggerConfCodes.as"] = WiredBoxFamily.Trigger,
        ["actiontypes/ActionTypeCodes.as"] = WiredBoxFamily.Action,
        ["conditions/ConditionCodes.as"] = WiredBoxFamily.Condition,
        ["selectors/SelectorCodes.as"] = WiredBoxFamily.Selector,
        ["addons/AddonCodes.as"] = WiredBoxFamily.Addon,
        ["variables/VariableCodes.as"] = WiredBoxFamily.Variable,
    };

    private static readonly Dictionary<string, WiredBoxFamily> VortexEnums = new(
        StringComparer.Ordinal
    )
    {
        ["WiredTriggerType"] = WiredBoxFamily.Trigger,
        ["WiredActionType"] = WiredBoxFamily.Action,
        ["WiredConditionType"] = WiredBoxFamily.Condition,
        ["WiredSelectorType"] = WiredBoxFamily.Selector,
        ["WiredAddonType"] = WiredBoxFamily.Addon,
        // WiredVariableBoxType, not WiredVariableType: the first names the box families the client
        // has forms for (Furni/User/Global/Context/...), the second names what kind of value a
        // variable holds. The names invite the wrong one, and picking it reports the whole variable
        // family as unimplemented.
        ["WiredVariableBoxType"] = WiredBoxFamily.Variable,
    };

    private const string ClientWiredSetup = "com/sulake/habbo/roomevents/wired_setup";

    private const string VortexEnumDirectory = "Vortex.Primitives/Rooms/Enums/Wired";

    private const string VortexLogicDirectory = "Vortex.Rooms/Object/Logic/Furniture/Floor/Wired";

    [GeneratedRegex(@"public\s+static\s+var\s+(\w+)\s*:\s*int\s*=\s*(-?\d+)\s*;")]
    private static partial Regex ClientCode();

    [GeneratedRegex(@"^\s*(\w+)\s*=\s*(-?\d+)\s*,?\s*$", RegexOptions.Multiline)]
    private static partial Regex EnumMember();

    [GeneratedRegex(@"WiredCode\s*=>\s*\(int\)\s*(\w+)\s*\.\s*(\w+)")]
    private static partial Regex VortexWiredCode();

    /// <summary>The client's constants for one family: code to name, obfuscated names included.</summary>
    public static IReadOnlyDictionary<int, string> ParseClientCodes(string text)
    {
        Dictionary<int, string> codes = [];

        foreach (Match match in ClientCode().Matches(text))
        {
            // First wins, so the table is a property of the file's order and not of ours. A repeated
            // code in the client's own constants would be its bug to report, not ours to average.
            codes.TryAdd(int.Parse(match.Groups[2].Value, Globalization()), match.Groups[1].Value);
        }

        return codes;
    }

    /// <summary>An enum body as member to value, for resolving a logic's <c>WiredCode</c>.</summary>
    public static IReadOnlyDictionary<string, int> ParseEnumMembers(string text)
    {
        Dictionary<string, int> members = new(StringComparer.Ordinal);

        foreach (Match match in EnumMember().Matches(text))
        {
            members[match.Groups[1].Value] = int.Parse(match.Groups[2].Value, Globalization());
        }

        return members;
    }

    /// <summary>
    /// True when the client's constant is a decompiler placeholder rather than a name.
    /// </summary>
    /// <remarks>
    /// Such a box is recorded as nameless. Carrying <c>_SafeStr_10393</c> forward as its name would
    /// put a string that means nothing in a column a reader takes for meaning.
    /// </remarks>
    public static bool IsObfuscated(string constantName) =>
        constantName.StartsWith("_Safe", StringComparison.Ordinal);

    /// <summary>The enum and member a logic class routes on, when it declares one.</summary>
    public static (string Enum, string Member)? ParseVortexWiredCode(string text)
    {
        Match match = VortexWiredCode().Match(text);

        return match.Success ? (match.Groups[1].Value, match.Groups[2].Value) : null;
    }

    /// <summary>
    /// Scores the box surface against the client of <paramref name="targetRevision"/>.
    /// </summary>
    /// <remarks>
    /// The revision is required, not discovered. More than one official client sits beside this
    /// checkout and their box codes are not comparable across builds — taking whichever came first
    /// would silently score this emulator against a client from another year.
    /// </remarks>
    public static WiredSurfaceReport Analyze(SpecWorkspace workspace, string targetRevision)
    {
        List<string> problems = [];

        SourceTree? client = workspace.Clients.FirstOrDefault(t =>
            t.Kind == SourceTreeKind.OfficialClient
            && string.Equals(t.Revision, targetRevision, StringComparison.Ordinal)
            && Directory.Exists(Path.Combine(t.Root, "src", ClientWiredSetup))
        );

        if (client is null)
        {
            problems.Add(
                $"no official client for {targetRevision} with a wired_setup package was found, "
                    + "so there is no box surface to score against"
            );

            return new WiredSurfaceReport
            {
                TargetRevision = null,
                Boxes = [],
                UnreachableInVortex = [],
                Problems = problems,
            };
        }

        Dictionary<(WiredBoxFamily, int), string?> clientCodes = [];

        foreach ((string relative, WiredBoxFamily family) in ClientFiles)
        {
            string path = Path.Combine(
                client.Root,
                "src",
                ClientWiredSetup.Replace('/', Path.DirectorySeparatorChar),
                relative.Replace('/', Path.DirectorySeparatorChar)
            );

            if (!File.Exists(path))
            {
                problems.Add($"{relative} is absent from {client.Revision}");
                continue;
            }

            foreach ((int code, string name) in ParseClientCodes(File.ReadAllText(path)))
            {
                clientCodes[(family, code)] = IsObfuscated(name) ? null : name;
            }
        }

        Dictionary<(WiredBoxFamily, int), (string Logic, string Member)> vortexCodes = ReadVortex(
            workspace.RepositoryRoot,
            problems
        );

        List<WiredBoxObligation> boxes =
        [
            .. clientCodes
                .Select(entry =>
                {
                    (WiredBoxFamily family, int code) = entry.Key;
                    bool bound = vortexCodes.TryGetValue(
                        entry.Key,
                        out (string Logic, string Member) vortex
                    );

                    return new WiredBoxObligation
                    {
                        Family = family,
                        Code = code,
                        ClientName = entry.Value,
                        VortexLogic = bound ? vortex.Logic : null,
                        VortexName = bound ? vortex.Member : null,
                    };
                })
                .OrderBy(b => b.Family)
                .ThenBy(b => b.Code),
        ];

        List<WiredBoxObligation> unreachable =
        [
            .. vortexCodes
                .Where(entry => !clientCodes.ContainsKey(entry.Key))
                .Select(entry => new WiredBoxObligation
                {
                    Family = entry.Key.Item1,
                    Code = entry.Key.Item2,
                    VortexLogic = entry.Value.Logic,
                    VortexName = entry.Value.Member,
                })
                .OrderBy(b => b.Family)
                .ThenBy(b => b.Code),
        ];

        return new WiredSurfaceReport
        {
            TargetRevision = client.Revision,
            Boxes = boxes,
            UnreachableInVortex = unreachable,
            Problems = [.. problems.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
        };
    }

    private static Dictionary<(WiredBoxFamily, int), (string Logic, string Member)> ReadVortex(
        string repositoryRoot,
        List<string> problems
    )
    {
        Dictionary<string, IReadOnlyDictionary<string, int>> enums = new(StringComparer.Ordinal);

        foreach ((string name, WiredBoxFamily _) in VortexEnums)
        {
            string path = Path.Combine(
                repositoryRoot,
                VortexEnumDirectory.Replace('/', Path.DirectorySeparatorChar),
                $"{name}.cs"
            );

            enums[name] = File.Exists(path)
                ? ParseEnumMembers(File.ReadAllText(path))
                : new Dictionary<string, int>(StringComparer.Ordinal);
        }

        Dictionary<(WiredBoxFamily, int), (string, string)> bound = [];
        string logicRoot = Path.Combine(
            repositoryRoot,
            VortexLogicDirectory.Replace('/', Path.DirectorySeparatorChar)
        );

        if (!Directory.Exists(logicRoot))
        {
            problems.Add($"{VortexLogicDirectory} is absent from this checkout");

            return bound;
        }

        foreach (
            string file in Directory
                .EnumerateFiles(logicRoot, "*.cs", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
        )
        {
            if (ParseVortexWiredCode(File.ReadAllText(file)) is not var (enumName, member))
            {
                continue;
            }

            if (
                !VortexEnums.TryGetValue(enumName, out WiredBoxFamily family)
                || !enums[enumName].TryGetValue(member, out int code)
            )
            {
                problems.Add(
                    $"{Path.GetFileNameWithoutExtension(file)} routes on {enumName}.{member}, "
                        + "which is not a wired family this analyzer knows"
                );

                continue;
            }

            // Two logics on one code would make the second unreachable; the engine binds by class
            // name, so the report says so rather than picking one.
            if (!bound.TryAdd((family, code), (Path.GetFileNameWithoutExtension(file), member)))
            {
                problems.Add(
                    $"{family.ToString().ToLowerInvariant()} code {code} is claimed by both "
                        + $"{bound[(family, code)].Item1} and {Path.GetFileNameWithoutExtension(file)}"
                );
            }
        }

        return bound;
    }

    private static System.Globalization.CultureInfo Globalization() =>
        System.Globalization.CultureInfo.InvariantCulture;
}
