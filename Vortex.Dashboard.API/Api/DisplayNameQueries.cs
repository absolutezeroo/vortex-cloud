using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Turns a set of player or room ids into the names an operator reads, for the many pages that show
/// an id alongside the thing it points at.
/// </summary>
/// <remarks>
/// <para>
/// Eight subjects already ask this same question, which is why it is one named thing rather than a
/// copy per subject. It is deliberately not a method on <see cref="DashboardReads"/>: that base
/// carries nothing but the context, and putting the first shared query on it is how it would become
/// the next catch-all. Extension methods on the context instead, so a read class that needs names
/// gets them without inheriting anything.
/// </para>
/// <para>
/// An empty id set short-circuits rather than issuing an <c>IN ()</c> query — the callers are
/// list pages where "no room pinned on any row" is the common case.
/// </para>
/// </remarks>
internal static class DisplayNameQueries
{
    /// <summary>Room id to room name, for the ids that still exist. A missing key means a deleted room.</summary>
    public static async Task<Dictionary<int, string>> RoomNamesAsync(
        this VortexDbContext db,
        IReadOnlyList<int> roomIds,
        CancellationToken ct
    ) =>
        roomIds.Count == 0
            ? []
            : await db
                .Rooms.AsNoTracking()
                .Where(r => roomIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name, ct)
                .ConfigureAwait(false);

    /// <summary>Player id to player name, for the ids that still exist.</summary>
    public static async Task<Dictionary<int, string>> PlayerNamesAsync(
        this VortexDbContext db,
        IReadOnlyList<int> playerIds,
        CancellationToken ct
    ) =>
        playerIds.Count == 0
            ? []
            : await db
                .Players.AsNoTracking()
                .Where(p => playerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct)
                .ConfigureAwait(false);
}
