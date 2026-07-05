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
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Moderator;

public class ModTradingLockMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events,
    IOptions<ModerationConfig> moderationConfig,
    ILogger<ModTradingLockMessageHandler> logger
) : IMessageHandler<ModTradingLockMessage>
{
    private readonly ModerationConfig _moderationConfig = moderationConfig.Value;

    public async ValueTask HandleAsync(
        ModTradingLockMessage message,
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
                    ModerationAction.TradingLock,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            DateTime? lockedUntil = ResolveLockExpiry(message.LockDurationTypeId);
            IPlayerGrain targetGrain = grainFactory.GetPlayerGrain(message.UserId);

            success = await targetGrain
                .ApplyTradingLockAsync(ctx.PlayerId, lockedUntil, ct)
                .ConfigureAwait(false);
        }

        await ModToolActionHelper
            .SendCautionIfPresentAsync(grainFactory, message.UserId, message.Message)
            .ConfigureAwait(false);
        await ModToolActionHelper
            .SendResultAsync(grainFactory, ctx.PlayerId, message.UserId, success)
            .ConfigureAwait(false);
    }

    private DateTime? ResolveLockExpiry(int lockDurationTypeId)
    {
        if (
            !_moderationConfig.TradingLockDurationHoursByType.TryGetValue(
                lockDurationTypeId,
                out int? hours
            )
        )
        {
            logger.LogWarning(
                "Unrecognized ModTradingLock lockDurationTypeId {LockDurationTypeId}; falling back to {FallbackHours}h",
                lockDurationTypeId,
                _moderationConfig.UnknownSanctionTypeFallbackHours
            );
            hours = _moderationConfig.UnknownSanctionTypeFallbackHours;
        }

        return hours is null ? AccountBan.Permanent : DateTime.UtcNow.AddHours(hours.Value);
    }
}
