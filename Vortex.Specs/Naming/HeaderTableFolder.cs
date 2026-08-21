using System;
using System.Collections.Generic;
using System.Linq;

namespace Vortex.Specs.Naming;

/// <summary>
/// Folds a source's <c>constant name to header id</c> table into one keyed by symbolic name.
/// </summary>
/// <remarks>
/// Two constants can canonicalize to the same symbolic name — Arcturus declares both
/// <c>RoomEntryInfoMessageComposer</c> and <c>RoomEntryInfoMessage</c>, and both reduce to
/// <c>RoomEntryInfo</c>. Folding with a plain dictionary insert throws on the second one, and folding
/// with last-write-wins silently picks whichever the file system enumerated last. Neither is
/// acceptable for a table this system then compares across sources, so a collision is resolved
/// deterministically and reported.
/// </remarks>
public static class HeaderTableFolder
{
    public sealed record Result(
        IReadOnlyDictionary<string, int> Table,
        IReadOnlyList<string> Collisions
    );

    public static Result Fold(IEnumerable<KeyValuePair<string, int>> constants)
    {
        Dictionary<string, int> table = new(StringComparer.Ordinal);
        Dictionary<string, string> winners = new(StringComparer.Ordinal);
        List<string> collisions = [];

        // Ordinal by constant name so the winner is a property of the data, not of enumeration order.
        foreach (
            KeyValuePair<string, int> constant in constants.OrderBy(
                c => c.Key,
                StringComparer.Ordinal
            )
        )
        {
            string canonical = PacketNaming.Canonical(constant.Key);

            if (!table.TryGetValue(canonical, out int existing))
            {
                table[canonical] = constant.Value;
                winners[canonical] = constant.Key;
                continue;
            }

            if (existing == constant.Value)
            {
                continue;
            }

            collisions.Add(
                $"{canonical}: {winners[canonical]}={existing} and {constant.Key}={constant.Value} "
                    + $"both reduce to the same symbolic name; kept {existing}"
            );
        }

        return new Result(table, collisions);
    }
}
