using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.PacketHandlers.Configuration;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Messages.Outgoing.Moderation;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;

namespace Turbo.PacketHandlers.Moderator;

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
