using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Server.Grains;

namespace Vortex.PacketHandlers.Moderator;

public class ModMuteMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<ModMuteMessage>
{
    public async ValueTask HandleAsync(
        ModMuteMessage message,
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
                    ModerationAction.Mute,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            int muteMinutes = await grainFactory
                .GetServerConfigGrain()
                .GetIntAsync(
                    ModerationConfig.ModToolDefaultMuteMinutesKey,
                    ModerationConfig.ModToolDefaultMuteMinutesDefault
                )
                .ConfigureAwait(false);

            // Hotel-wide, not room-scoped. The mod tool's mute is a sanction on the person: a room
            // mute would end the moment they walked next door, and would be impossible to apply at
            // all to somebody sitting in the hotel view with no room to scope it to.
            DateTime mutedUntil = DateTime.UtcNow.AddMinutes(muteMinutes);

            DateTime? applied = await grainFactory
                .GetPlayerGrain(message.UserId)
                .ApplyHotelMuteAsync(ctx.PlayerId, mutedUntil, ct)
                .ConfigureAwait(false);

            success = applied is not null;

            // If they are standing somewhere, that room is holding a cached copy from their entry
            // snapshot; without this the sanction would not bite until their next room change.
            if (success && targetRoomId > 0)
            {
                await grainFactory
                    .GetRoomModeration(targetRoomId)
                    .SetHotelMuteAsync(message.UserId, applied)
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
