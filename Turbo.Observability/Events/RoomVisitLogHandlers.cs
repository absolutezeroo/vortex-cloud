using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Room;
using Turbo.Events.Registry;
using Turbo.Primitives.Events;

namespace Turbo.Observability.Events;

/// <summary>
/// Persists the structured room-visit history the staff moderation tool's
/// GetRoomVisitsMessageHandler/GetRoomChatlogMessageHandler need (a real append-only log, not
/// something to reconstruct by parsing the generic <see cref="Turbo.Primitives.Observability.AuditEvent"/>
/// JSON blob that <see cref="SessionLifecycleAuditHandlers"/> also writes from these same events).
/// </summary>
public sealed class RoomVisitLogEnteredHandler(IDbContextFactory<TurboDbContext> dbCtxFactory)
    : IEventHandler<PlayerEnteredRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerEnteredRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        dbCtx.RoomEntryLogs.Add(
            new RoomEntryLogEntity { RoomEntityId = e.RoomId, PlayerEntityId = e.PlayerId }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class RoomVisitLogLeftHandler(IDbContextFactory<TurboDbContext> dbCtxFactory)
    : IEventHandler<PlayerLeftRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerLeftRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        RoomEntryLogEntity? openVisit = await dbCtx
            .RoomEntryLogs.Where(v =>
                v.PlayerEntityId == e.PlayerId.Value
                && v.RoomEntityId == e.RoomId.Value
                && v.ExitedAt == null
            )
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (openVisit is null)
        {
            return;
        }

        openVisit.ExitedAt = e.LeftAtUtc;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
