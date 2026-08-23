using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Events;
using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Moderator;

public class ModTradingLockMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
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
            // The client sends the length itself (actionLengthHours * 60) rather than an index into
            // the server's preset table, so there is nothing to resolve here. A non-positive value
            // is refused rather than silently promoted to a permanent lock.
            if (message.DurationMinutes <= 0)
            {
                logger.LogWarning(
                    "ModTradingLock for {UserId} carried a non-positive duration ({DurationMinutes} min); lock rejected.",
                    message.UserId,
                    message.DurationMinutes
                );
            }
            else
            {
                DateTime lockedUntil = DateTime.UtcNow.AddMinutes(message.DurationMinutes);

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
