using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Preferences;

public class SetChatStylePreferenceMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetChatStylePreferenceMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SetChatStylePreferenceMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // Orchestration-only: the grain owns the persistence (and no-ops when unchanged). FontSizeMode
        // is a client-side render preference with no server-side storage, so it is intentionally not
        // persisted here.
        await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .SetChatStylePreferenceAsync(message.ChatStyle, ct)
            .ConfigureAwait(false);
    }
}
