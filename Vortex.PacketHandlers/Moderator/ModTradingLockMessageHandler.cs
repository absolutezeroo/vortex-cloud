using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Moderator;

public class ModTradingLockMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ISanctionPresetService sanctionPresets,
    IEventPublisher events,
    ILogger<ModTradingLockMessageHandler> logger
) : IMessageHandler<ModTradingLockMessage>
{
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
            SanctionPresetSnapshot? preset = await sanctionPresets
                .ResolveAsync(SanctionPresetKind.TradingLock, message.LockDurationTypeId, ct)
                .ConfigureAwait(false);

            if (preset is null)
            {
                logger.LogWarning(
                    "No SanctionPresetEntity configured for TradingLock preset index {LockDurationTypeId}; lock rejected.",
                    message.LockDurationTypeId
                );
            }
            else
            {
                DateTime lockedUntil = preset.Value.DurationSeconds is null
                    ? SanctionDuration.Permanent
                    : DateTime.UtcNow.AddSeconds(preset.Value.DurationSeconds.Value);

                IPlayerGrain targetGrain = grainFactory.GetPlayerGrain(message.UserId);

                success = await targetGrain
                    .ApplyTradingLockAsync(ctx.PlayerId, lockedUntil, ct)
                    .ConfigureAwait(false);
            }
        }

        await ModToolActionHelper
            .SendCautionIfPresentAsync(grainFactory, message.UserId, message.Message)
            .ConfigureAwait(false);
        await ModToolActionHelper
            .SendResultAsync(grainFactory, ctx.PlayerId, message.UserId, success)
            .ConfigureAwait(false);
    }
}
