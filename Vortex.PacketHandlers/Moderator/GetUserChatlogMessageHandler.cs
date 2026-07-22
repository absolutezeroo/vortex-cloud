using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Server.Grains;

namespace Vortex.PacketHandlers.Moderator;

public class GetUserChatlogMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IModeratorChatlogService chatlogService
) : IMessageHandler<GetUserChatlogMessage>
{
    public async ValueTask HandleAsync(
        GetUserChatlogMessage message,
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

        IServerConfigGrain config = grainFactory.GetServerConfigGrain();
        int roomLimit = await config
            .GetIntAsync(
                ModerationConfig.UserChatlogRoomLimitKey,
                ModerationConfig.UserChatlogRoomLimitDefault
            )
            .ConfigureAwait(false);
        int messagesPerRoom = await config
            .GetIntAsync(
                ModerationConfig.UserChatlogMessagesPerRoomKey,
                ModerationConfig.UserChatlogMessagesPerRoomDefault
            )
            .ConfigureAwait(false);

        ImmutableArray<ChatlogBlockSnapshot> rooms = await chatlogService
            .GetUserChatlogAsync(message.UserId, roomLimit, messagesPerRoom, ct)
            .ConfigureAwait(false);

        PlayerSummarySnapshot targetSummary = await grainFactory
            .GetPlayerGrain(message.UserId)
            .GetSummaryAsync(ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new UserChatlogEventMessageComposer
                {
                    UserId = message.UserId,
                    UserName = targetSummary.Name,
                    Rooms = rooms,
                }
            )
            .ConfigureAwait(false);
    }
}
