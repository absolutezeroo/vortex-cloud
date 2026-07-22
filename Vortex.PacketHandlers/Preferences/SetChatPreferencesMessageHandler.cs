using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Preferences;

public class SetChatPreferencesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetChatPreferencesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SetChatPreferencesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .SetFreeFlowChatDisabledAsync(message.FreeFlowChatDisabled, ct)
            .ConfigureAwait(false);
    }
}
