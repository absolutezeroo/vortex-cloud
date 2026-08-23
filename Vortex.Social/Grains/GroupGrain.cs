using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Groups;
using Vortex.Primitives.Events;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Grains;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Players;

namespace Vortex.Social.Grains;

/// <summary>
/// One guild. This grain holds no in-memory state — every operation opens its own short-lived
/// <see cref="VortexDbContext"/> — so it is a serialization point per guild rather than a cache:
/// Orleans runs its calls one at a time, which is what keeps concurrent joins and rank changes on
/// the same guild from interleaving.
/// </summary>
/// <remarks>
/// Split across partial files by concern: <c>.Read</c> (queries the client polls), <c>.Membership</c>
/// (who belongs and at what rank), <c>.Settings</c> (guild identity and lifecycle), and
/// <c>.Notifications</c> (pushes to admins and to the base room). Shared authorization lives here so
/// there is exactly one answer to "may this actor administer this guild".
/// </remarks>
internal sealed partial class GroupGrain : Grain, IGroupGrain
{
    // Join failure reason code (client HabboGroupJoinFailedMessageEvent).
    private const int JoinFailedNotOpen = 2;

    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory;
    private readonly IEventPublisher _events;
    private readonly ILogger<GroupGrain> _logger;
    private readonly IVortexMetrics _metrics;

    public GroupGrain(
        IDbContextFactory<VortexDbContext> dbCtxFactory,
        IEventPublisher events,
        ILogger<GroupGrain> logger,
        IVortexMetrics metrics
    )
    {
        _dbCtxFactory = dbCtxFactory;
        _events = events;
        _logger = logger;
        _metrics = metrics;
    }

    private int GroupId => (int)this.GetPrimaryKeyLong();

    /// <summary>
    /// Whether the actor may administer this guild. The owner always may, and is deliberately not
    /// required to also hold an admin rank row.
    /// </summary>
    private async Task<bool> IsAdminAsync(
        VortexDbContext dbCtx,
        GroupEntity group,
        int actorId,
        CancellationToken ct
    )
    {
        if (group.OwnerPlayerEntityId == actorId)
        {
            return true;
        }

        return await dbCtx.GroupMembers.AnyAsync(
            m =>
                m.GroupEntityId == GroupId
                && m.PlayerEntityId == actorId
                && m.Rank == GroupMemberRank.Admin
                && m.DeletedAt == null,
            ct
        );
    }

    /// <summary>Loads the (tracked) group iff the actor is owner or admin; else null.</summary>
    private async Task<GroupEntity?> LoadIfAdminAsync(
        VortexDbContext dbCtx,
        PlayerId actor,
        CancellationToken ct
    )
    {
        GroupEntity? group = await dbCtx.Groups.FirstOrDefaultAsync(
            g => g.Id == GroupId && g.DeletedAt == null,
            ct
        );

        if (group is null)
        {
            return null;
        }

        return await IsAdminAsync(dbCtx, group, actor.Value, ct).ConfigureAwait(true)
            ? group
            : null;
    }
}
