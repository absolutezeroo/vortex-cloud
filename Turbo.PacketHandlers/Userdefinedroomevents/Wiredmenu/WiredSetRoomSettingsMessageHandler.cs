using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredSetRoomSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredSetRoomSettingsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredSetRoomSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);

        WiredRoomSettingsEventMessageComposer? settings = await room.SetWiredRoomSettingsAsync(
                new PlayerId(ctx.PlayerId),
                message.ModifyPermissionMask,
                message.ReadPermissionMask,
                message.Timezone,
                ct
            )
            .ConfigureAwait(false);

        if (settings is null)
        {
            return;
        }

        _ = ctx.SendComposerAsync(settings, ct).ConfigureAwait(false);
    }
}
