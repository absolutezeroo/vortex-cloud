using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.PacketHandlers.Configuration;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Messages.Outgoing.Moderation;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Moderator;

public class ModBanMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events,
    ISessionGateway sessionGateway,
    IOptions<ModerationConfig> moderationConfig,
    ILogger<ModBanMessageHandler> logger
) : IMessageHandler<ModBanMessage>
{
    private readonly ModerationConfig _moderationConfig = moderationConfig.Value;

    public async ValueTask HandleAsync(
        ModBanMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0)
        {
            return;
        }

        RoomId targetRoomId = await ModToolActionHelper
            .GetTargetRoomIdAsync(grainFactory, message.UserId)
            .ConfigureAwait(false);

        bool success = false;

        if (
            await ModToolActionHelper
                .IsAuthorizedAsync(
                    permissionService,
                    events,
                    ctx.PlayerId,
                    message.UserId,
                    targetRoomId,
                    ModerationAction.Ban,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            DateTime? bannedUntil = ResolveBanExpiry(message.SanctionTypeId);
            IPlayerGrain targetGrain = grainFactory.GetPlayerGrain(message.UserId);

            success = await targetGrain
                .ApplyAccountBanAsync(ctx.PlayerId, bannedUntil, message.Message, ct)
                .ConfigureAwait(false);

            if (success)
            {
                await grainFactory
                    .GetPlayerPresenceGrain(message.UserId)
                    .SendComposerAsync(new UserBannedMessageComposer { Message = message.Message })
                    .ConfigureAwait(false);
                await sessionGateway
                    .RemoveSessionFromPlayerAsync(message.UserId, ct)
                    .ConfigureAwait(false);
            }
        }

        await ModToolActionHelper
            .SendResultAsync(grainFactory, ctx.PlayerId, message.UserId, success)
            .ConfigureAwait(false);
    }

    private DateTime? ResolveBanExpiry(int sanctionTypeId)
    {
        if (
            !_moderationConfig.BanDurationHoursBySanctionType.TryGetValue(
                sanctionTypeId,
                out int? hours
            )
        )
        {
            logger.LogWarning(
                "Unrecognized ModBan sanctionTypeId {SanctionTypeId}; falling back to {FallbackHours}h",
                sanctionTypeId,
                _moderationConfig.UnknownSanctionTypeFallbackHours
            );
            hours = _moderationConfig.UnknownSanctionTypeFallbackHours;
        }

        return hours is null ? AccountBan.Permanent : DateTime.UtcNow.AddHours(hours.Value);
    }
}
