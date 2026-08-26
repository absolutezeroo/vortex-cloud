using System;
using System.Collections.Generic;

namespace Vortex.Primitives.Furniture;

/// <summary>
/// Reads the hand-item list an operator typed into a vending machine's definition.
/// </summary>
/// <remarks>
/// <para>
/// The column is free text, filled in by hand through the furniture admin page, so this is a trust
/// boundary rather than a formality. It accepts what an operator plausibly types — commas,
/// semicolons, spaces, in any mixture — and silently drops anything that is not a positive number,
/// because the alternative is a definition that refuses to load and takes the furniture cache with
/// it.
/// </para>
/// <para>
/// Empty in, empty out. That is the normal state: nothing in the official client, the furnidata or
/// any capture says which drink a given machine dispenses, so every machine is unconfigured until
/// somebody decides.
/// </para>
/// </remarks>
public static class VendingIdParser
{
    private static readonly char[] Separators = [',', ';', ' ', '\t', '\n', '\r'];

    public static IReadOnlyList<int> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        List<int> ids = [];

        foreach (string part in raw.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            // Duplicates are kept on purpose: repeating an id is how an operator weights it, which
            // is the only way to say "mostly water, sometimes champagne" with a flat list.
            if (int.TryParse(part, out int id) && id > 0)
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
