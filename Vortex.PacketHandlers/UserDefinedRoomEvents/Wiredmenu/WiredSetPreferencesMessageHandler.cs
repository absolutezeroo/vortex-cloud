using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredSetPreferencesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredSetPreferencesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredSetPreferencesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .SetWiredPreferencesAsync(
                new PlayerWiredPreferencesSnapshot
                {
                    WiredMenuButton = message.WiredMenuButton,
                    WiredInspectButton = message.WiredInspectButton,
                    PlayTestMode = message.PlayTestMode,
                    WiredWhisperDisabled = message.WiredWhisperDisabled,
                    ShowAllNotifications = message.ShowAllNotifications,
                    UiStyle = message.UiStyle,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
