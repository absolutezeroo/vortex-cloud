using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Preferences;

public class SetSoundSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetSoundSettingsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SetSoundSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // The client's "generic" volume is the UI/system channel; trax and furni are the other two.
        await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .SetSoundSettingsAsync(
                uiVolume: message.Generic,
                furniVolume: message.Furni,
                traxVolume: message.Trax,
                ct
            )
            .ConfigureAwait(false);
    }
}
