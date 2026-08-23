using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Snapshots.Wired;

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

        IRoomWired room = _grainFactory.GetRoomWired(ctx.RoomId);

        WiredRoomSettingsSnapshot? settings = await room.SetWiredRoomSettingsAsync(
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
