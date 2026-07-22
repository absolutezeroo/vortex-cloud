using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Server.Grains;

namespace Vortex.PacketHandlers.Moderator;

public class GetRoomChatlogMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IModeratorChatlogService chatlogService
) : IMessageHandler<GetRoomChatlogMessage>
{
    public async ValueTask HandleAsync(
        GetRoomChatlogMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
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
            .GetIntAsync(
                ModerationConfig.RoomChatlogLimitKey,
                ModerationConfig.RoomChatlogLimitDefault
            )
            .ConfigureAwait(false);

        ChatlogBlockSnapshot block = await chatlogService
            .GetRoomChatlogAsync(message.RoomId, limit, ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new RoomChatlogEventMessageComposer { Block = block })
            .ConfigureAwait(false);
    }
}
