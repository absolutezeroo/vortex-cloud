using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetRoomSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetRoomSettingsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetRoomSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredRoomSettingsSnapshot settings = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetWiredRoomSettingsAsync(new PlayerId(ctx.PlayerId), ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new WiredRoomSettingsEventMessageComposer
                {
                    ModifyPermissionMask = settings.ModifyPermissionMask,
                    ReadPermissionMask = settings.ReadPermissionMask,
                    Timezone = settings.Timezone,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
