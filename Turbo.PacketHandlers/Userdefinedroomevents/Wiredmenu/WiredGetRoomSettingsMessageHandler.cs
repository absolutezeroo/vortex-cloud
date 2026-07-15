using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;

namespace Turbo.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

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

        WiredRoomSettingsEventMessageComposer settings = await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .GetWiredRoomSettingsAsync(new PlayerId(ctx.PlayerId), ct)
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(settings, ct).ConfigureAwait(false);
    }
}
