using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Server.Grains;
using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Protocol.Messages.Outgoing.Moderation;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// Where a user has been. Gated behind the same capability as the chatlogs — a visit history is
/// surveillance data about a player, not public room information.
/// </summary>
public class GetRoomVisitsMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IModeratorRoomVisitService roomVisitService
) : IMessageHandler<GetRoomVisitsMessage>
{
    public async ValueTask HandleAsync(
        GetRoomVisitsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.HasAny(Capabilities.Moderation.Chatlogs, Capabilities.Room.ModerateAny))
        {
            return;
        }

        int limit = await grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(ModerationConfig.RoomVisitLimitKey, ModerationConfig.RoomVisitLimitDefault)
            .ConfigureAwait(false);

        RoomVisitHistorySnapshot history = await roomVisitService
            .GetUserRoomVisitsAsync(message.UserId, limit, ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new RoomVisitsEventMessageComposer
                {
                    UserId = history.UserId,
                    UserName = history.UserName,
                    Visits = history.Visits,
                }
            )
            .ConfigureAwait(false);
    }
}
