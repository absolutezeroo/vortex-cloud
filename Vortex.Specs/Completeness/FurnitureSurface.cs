using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vortex.Specs.Completeness;

/// <summary>One logic name the shipped assets bind furniture to, and whether Vortex answers to it.</summary>
public sealed record FurnitureLogicObligation
{
    public required string Logic { get; init; }

    /// <summary>How many definitions the binding pass assigns this logic.</summary>
    public required int Definitions { get; init; }

    /// <summary>The Vortex classes registered under this name, floor and wall alike.</summary>
    public IReadOnlyList<string> RegisteredBy { get; init; } = [];

    public bool Registered => RegisteredBy.Count > 0;
}

public sealed record FurnitureSurfaceReport
{
    public required string SourceFile { get; init; }

    public required IReadOnlyList<FurnitureLogicObligation> Logics { get; init; }

    public required IReadOnlyList<string> Problems { get; init; }

    public bool HasSource => Logics.Count > 0;

    public int Definitions => Logics.Sum(l => l.Definitions);

    /// <summary>
    /// Definitions whose logic name nothing here answers to. They resolve to the family default and
    /// simply never do their thing — no error, no warning a player would see.
    /// </summary>
    public int Stranded => Logics.Where(l => !l.Registered).Sum(l => l.Definitions);

    public string Share =>
        Definitions == 0
            ? "n/a"
            : string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} / {1} ({2:0.0}%)",
                Definitions - Stranded,
                Definitions,
                100.0 * (Definitions - Stranded) / Definitions
            );
}

/// <summary>
/// The furniture logic surface: which behaviours the shipped assets ask for, and which of those this
/// emulator registers.
/// </summary>
/// <remarks>
/// A third denominator, and the one with the widest blast radius per gap. A missing packet costs one
/// interaction; a logic name nothing binds costs every definition carrying it, silently — the furni
/// resolves to its family default, places, sits there and does nothing. Ranked by definitions
/// impacted, because that is the only thing that separates an intentional fallback from an accident.
/// <para>
/// The denominator is the committed asset-derived binding pass, not the live database. Definitions
/// that already carried a registered Vortex logic are absent from it by design — which is exactly
/// right here, since those are the ones that were never at risk.
/// </para>
/// </remarks>
public static partial class FurnitureSurfaceAnalyzer
{
    public const string SeedFile = "Vortex.Database/Seeds/furni_logic_bindings.sql";

    private const string LogicDirectory = "Vortex.Rooms/Object/Logic";

    /// <summary>The <c>-- N definitions</c> banner each binding statement is preceded by.</summary>
    [GeneratedRegex(@"^--\s*(\d+)\s+definitions\s*$", RegexOptions.Multiline)]
    private static partial Regex DefinitionCount();

    /// <remarks>
    /// <c>(?:[^']|'')*</c> rather than a lazy <c>.*?</c> anchored to the line end. The lazy form
    /// worked only because the anchor forced it to backtrack past a doubled quote, so it read
    /// <c>Chama D''agua</c> correctly and would have silently truncated it to <c>Chama D</c> the day
    /// the generator put the statement on one line. Spelling the escape out costs nothing and does
    /// not depend on where the line happens to break.
    /// </remarks>
    [GeneratedRegex(@"SET\s+`logic`\s*=\s*'((?:[^']|'')*)'")]
    private static partial Regex LogicAssignment();

    [GeneratedRegex(@"\[RoomObjectLogic\(""([^""]+)""\)\]")]
    private static partial Regex RegisteredLogic();

    /// <summary>
    /// Reads the seed as logic name to definition count.
    /// </summary>
    /// <remarks>
    /// The count comes from the banner the generator writes, not from counting the names in the
    /// <c>IN</c> list: classnames repeat in that list because a classname is not a key, so counting
    /// entries would undercount the definitions each statement actually touches.
    /// </remarks>
    public static IReadOnlyDictionary<string, int> ParseSeedBindings(string sql)
    {
        MatchCollection counts = DefinitionCount().Matches(sql);
        MatchCollection logics = LogicAssignment().Matches(sql);
        Dictionary<string, int> bindings = new(StringComparer.Ordinal);

        foreach (Match logic in logics)
        {
            // The banner immediately above this assignment. Pairing by position rather than by
            // parsing the whole statement keeps this readable and is exactly how the file is laid
            // out; a statement with no banner above it contributes no count rather than a guess.
            Match? banner = counts
                .Where(c => c.Index < logic.Index)
                .OrderByDescending(c => c.Index)
                .FirstOrDefault();

            if (banner is null)
            {
                continue;
            }

            // SQL escapes a quote by doubling it. Undo that so the name matches the asset's.
            string name = logic.Groups[1].Value.Replace("''", "'", StringComparison.Ordinal);
            int count = int.Parse(
                banner.Groups[1].Value,
                System.Globalization.CultureInfo.InvariantCulture
            );

            bindings[name] = bindings.TryGetValue(name, out int existing)
                ? existing + count
                : count;
        }

        return bindings;
    }

    /// <summary>Every logic name a class in <paramref name="text"/> registers.</summary>
    public static IReadOnlyList<string> ParseRegisteredLogics(string text) =>
        [.. RegisteredLogic().Matches(text).Select(m => m.Groups[1].Value)];

    public static FurnitureSurfaceReport Analyze(string repositoryRoot)
    {
        List<string> problems = [];
        string seedPath = Path.Combine(
            repositoryRoot,
            SeedFile.Replace('/', Path.DirectorySeparatorChar)
        );

        if (!File.Exists(seedPath))
        {
            problems.Add(
                $"{SeedFile} is absent from this checkout, so there is no asset-derived binding "
                    + "pass to score against"
            );

            return new FurnitureSurfaceReport
            {
                SourceFile = SeedFile,
                Logics = [],
                Problems = problems,
            };
        }

        IReadOnlyDictionary<string, int> assets = ParseSeedBindings(File.ReadAllText(seedPath));
        Dictionary<string, List<string>> registered = new(StringComparer.Ordinal);
        string logicRoot = Path.Combine(
            repositoryRoot,
            LogicDirectory.Replace('/', Path.DirectorySeparatorChar)
        );

        if (!Directory.Exists(logicRoot))
        {
            problems.Add($"{LogicDirectory} is absent from this checkout");
        }
        else
        {
            foreach (
                string file in Directory
                    .EnumerateFiles(logicRoot, "*.cs", SearchOption.AllDirectories)
                    .OrderBy(f => f, StringComparer.Ordinal)
            )
            {
                foreach (string key in ParseRegisteredLogics(File.ReadAllText(file)))
                {
                    if (!registered.TryGetValue(key, out List<string>? classes))
                    {
                        classes = [];
                        registered[key] = classes;
                    }

                    classes.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }

        return new FurnitureSurfaceReport
        {
            SourceFile = SeedFile,
            Logics =
            [
                .. assets
                    .Select(entry => new FurnitureLogicObligation
                    {
                        Logic = entry.Key,
                        Definitions = entry.Value,
                        RegisteredBy = registered.TryGetValue(entry.Key, out List<string>? classes)
                            ? classes
                            : [],
                    })
                    // Worst first, and "worst" is definitions impacted. A name on two definitions
                    // and a name on two thousand are not the same finding.
                    .OrderByDescending(l => l.Registered ? 0 : l.Definitions)
                    .ThenByDescending(l => l.Definitions)
                    .ThenBy(l => l.Logic, StringComparer.Ordinal),
            ],
            Problems = [.. problems],
        };
    }
}
