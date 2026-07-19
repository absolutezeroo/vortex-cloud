using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;

namespace Vortex.PacketHandlers.Moderator;

public class GetRoomChatlogMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IModeratorChatlogService chatlogService,
    IOptions<ModerationConfig> moderationConfig
) : IMessageHandler<GetRoomChatlogMessage>
{
    private readonly ModerationConfig _moderationConfig = moderationConfig.Value;

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

        ChatlogBlockSnapshot block = await chatlogService
            .GetRoomChatlogAsync(message.RoomId, _moderationConfig.RoomChatlogLimit, ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new RoomChatlogEventMessageComposer { Block = block })
            .ConfigureAwait(false);
    }
}
