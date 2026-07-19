using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

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
