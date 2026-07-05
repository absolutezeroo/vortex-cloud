using System.Collections.Immutable;
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
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Players.Grains;

namespace Turbo.PacketHandlers.Moderator;

public class GetUserChatlogMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IModeratorChatlogService chatlogService,
    IOptions<ModerationConfig> moderationConfig
) : IMessageHandler<GetUserChatlogMessage>
{
    private readonly ModerationConfig _moderationConfig = moderationConfig.Value;

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

        ImmutableArray<ChatlogBlockSnapshot> rooms = await chatlogService
            .GetUserChatlogAsync(
                message.UserId,
                _moderationConfig.UserChatlogRoomLimit,
                _moderationConfig.UserChatlogMessagesPerRoom,
                ct
            )
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
