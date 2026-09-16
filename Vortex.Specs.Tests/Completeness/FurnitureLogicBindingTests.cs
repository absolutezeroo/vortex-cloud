using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Vortex.Specs.Completeness;
using Vortex.Specs.Sources;
using Xunit;

namespace Vortex.Specs.Tests.Completeness;

/// <summary>
/// The generated binding pass must not take a furni's own behaviour away from it.
/// </summary>
/// <remarks>
/// <para>
/// <c>furni_logic_bindings.sql</c> is generated from the shipped assets and writes per classname.
/// The client has no logic for behaviour that is purely server-side, so its assets call a wired box
/// or an invisible click tile <c>furniture_multistate</c> / <c>furniture_basic</c> — and whenever
/// this emulator registers a <c>[RoomObjectLogic]</c> key under a name the pass already lists,
/// that furni is rebound to the plain floor logic and silently stops working. It resolves cleanly,
/// places, and does nothing; no warning fires, because the logic it ends up with is real.
/// </para>
/// <para>
/// Three rounds of that have been repaired by hand. This is the check instead: every registered
/// logic key the generated pass rebinds must be listed in
/// <c>furni_logic_self_named_rebind.sql</c>, which the migration path applies afterwards. Register a
/// new logic under a classname the pass touches and this fails naming it, which is the whole point —
/// nothing else in the build, the tests or the runtime would say a word.
/// </para>
/// </remarks>
public class FurnitureLogicBindingTests
{
    private const string GeneratedPass = "Vortex.Database/Seeds/furni_logic_bindings.sql";

    private const string Rebind = "Vortex.Database/Seeds/furni_logic_self_named_rebind.sql";

    private const string LogicDirectory = "Vortex.Rooms/Object/Logic";

    [Fact]
    public void EveryLogicKeyTheGeneratedPassRebinds_IsPutBackByTheRebindSeed()
    {
        string root = SpecWorkspace.Discover(AppContext.BaseDirectory).RepositoryRoot;

        IReadOnlyDictionary<string, string> bound = EffectiveBindings(Read(root, GeneratedPass));
        IReadOnlyCollection<string> putBack = NamesIn(Read(root, Rebind));

        List<string> stranded =
        [
            .. RegisteredKeys(root)
                .Where(key =>
                    bound.TryGetValue(key, out string? logic)
                    && !string.Equals(logic, key, StringComparison.Ordinal)
                    && !putBack.Contains(key)
                )
                .OrderBy(key => key, StringComparer.Ordinal),
        ];

        stranded
            .Should()
            .BeEmpty(
                "these classnames register a room-object logic of their own, and the generated pass "
                    + "rebinds them to a plain floor logic — add them to {0}",
                Rebind
            );
    }

    /// <summary>
    /// The file with its <c>--</c> comments dropped. Both headers explain themselves by quoting the
    /// statements they are about, and a quoted <c>WHERE name IN (...)</c> reads exactly like the
    /// real thing to everything below.
    /// </summary>
    private static string Read(string root, string relativePath)
    {
        string sql = File.ReadAllText(
            Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        return string.Join(
            '\n',
            sql.Split('\n')
                .Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal))
        );
    }

    /// <summary>
    /// Classname to the logic the pass leaves it holding. Statements are read in file order and a
    /// later one wins, which is how the file itself behaves when MySQL runs it.
    /// </summary>
    private static IReadOnlyDictionary<string, string> EffectiveBindings(string sql)
    {
        Dictionary<string, string> bindings = new(StringComparer.Ordinal);

        // Split rather than one big regex: the file is ~0.9 MB and a lazy match across it is slow
        // enough to notice. Each piece is one statement's body, so the parse stays linear.
        foreach (
            string statement in sql.Split(
                "UPDATE `furniture_definitions`",
                StringSplitOptions.RemoveEmptyEntries
            )
        )
        {
            Match logic = Regex.Match(statement, @"SET\s+`logic`\s*=\s*'([^']+)'");

            if (!logic.Success)
            {
                continue;
            }

            foreach (string name in NamesIn(statement))
            {
                bindings[name] = logic.Groups[1].Value;
            }
        }

        return bindings;
    }

    /// <summary>
    /// The classnames of one statement's <c>WHERE name IN (...)</c> list: every quoted value after
    /// the opening bracket, to the end of the statement.
    /// </summary>
    /// <remarks>
    /// Not "up to the closing bracket". Classnames contain brackets — <c>Mut_Lampara_Navidad (1)</c>
    /// is one of them, 97 KB into a 700 KB list — so stopping at the first <c>)</c> reads a
    /// twentieth of the statement and reports the rest as unbound. Nothing follows the list inside a
    /// statement but the bracket and an unquoted condition, so reading to the end is both simpler
    /// and right.
    /// </remarks>
    private static HashSet<string> NamesIn(string statement)
    {
        int open = statement.IndexOf("IN (", StringComparison.Ordinal);

        if (open < 0)
        {
            return [];
        }

        return
        [
            .. Regex.Matches(statement[(open + 4)..], @"'([^']*)'").Select(m => m.Groups[1].Value),
        ];
    }

    private static IEnumerable<string> RegisteredKeys(string root)
    {
        string logicRoot = Path.Combine(
            root,
            LogicDirectory.Replace('/', Path.DirectorySeparatorChar)
        );

        return Directory
            .EnumerateFiles(logicRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file =>
                FurnitureSurfaceAnalyzer.ParseRegisteredLogics(File.ReadAllText(file))
            )
            .Distinct(StringComparer.Ordinal);
    }
}
