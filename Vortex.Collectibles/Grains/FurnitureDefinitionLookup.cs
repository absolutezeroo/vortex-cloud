using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;

namespace Vortex.Collectibles.Grains;

/// <summary>
/// Resolves furniture classnames to definition rows for the collectibles grains, which all store
/// product codes rather than definition ids.
/// </summary>
/// <remarks>
/// <para>
/// A classname is <b>not</b> a key. The unique index on <c>furniture_definitions</c> is
/// <c>(sprite_id, type, category)</c>, and the duplication is not an import fault: the client's own
/// furnidata ships the same classname more than once. <c>clothing_nftshoulderdragon1</c> is both id
/// 4197734 and id 9745384 there, and 3533 classnames are duplicated across 7463 live rows.
/// </para>
/// <para>
/// Keying a dictionary on the name therefore throws, and because every one of these grains catches
/// on load and empties its cache, a single duplicated code takes the whole surface down —
/// "Failed to load collectible collections" with nothing shown, for every player.
/// </para>
/// <para>
/// Collapsing on the lowest id keeps the pick deterministic across reloads. The duplicates observed
/// differ only in id and sprite id — same logic, size, states and behaviour — so <i>which</i> row
/// wins matters far less than it being the same row every time.
/// </para>
/// </remarks>
internal static class FurnitureDefinitionLookup
{
    /// <param name="project">
    /// Builds the cached value from the winning row. Kept generic because each grain caches a
    /// different shape — sprite id and type here, definition id as well there.
    /// </param>
    public static async Task<Dictionary<string, TValue>> ResolveByClassNameAsync<TValue>(
        VortexDbContext dbCtx,
        IEnumerable<string?> productCodes,
        Func<FurnitureDefinitionEntity, TValue> project,
        CancellationToken ct,
        ILogger? logger = null
    )
    {
        string[] codes =
        [
            .. productCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!)
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];

        if (codes.Length == 0)
        {
            return new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
        }

        List<FurnitureDefinitionEntity> rows = await dbCtx
            .FurnitureDefinitions.AsNoTracking()
            .Where(definition => codes.Contains(definition.Name) && definition.DeletedAt == null)
            .OrderBy(definition => definition.Id)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        Dictionary<string, TValue> resolved = new(StringComparer.OrdinalIgnoreCase);
        List<string> collapsed = [];

        foreach (
            IGrouping<string, FurnitureDefinitionEntity> group in rows.GroupBy(
                definition => definition.Name,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            // Ordered by id above, so the first of a group is the lowest.
            resolved.Add(group.Key, project(group.First()));

            if (group.Skip(1).Any())
            {
                collapsed.Add(group.Key);
            }
        }

        // Not an error — the data is a faithful mirror of furnidata — but picking one of several
        // definitions should never be invisible when somebody is working out why an item draws with
        // the sprite it does.
        if (collapsed.Count > 0 && logger is not null)
        {
            logger.LogDebug(
                "Resolved {DuplicateCount} classname(s) to their lowest-id definition because furnidata "
                    + "defines each more than once: {ClassNames}.",
                collapsed.Count,
                string.Join(", ", collapsed)
            );
        }

        return resolved;
    }
}
