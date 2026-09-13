using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Protocol.Messages.Incoming.Preferences;

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

        IPlayerGrain player = _grainFactory.GetPlayerGrain(ctx.PlayerId);

        await player
            .SetFreeFlowChatDisabledAsync(message.FreeFlowChatDisabled, ct)
            .ConfigureAwait(false);

        await player
            .SetChatDisplayPreferencesAsync(
                message.ChatMode,
                message.ChatBubbleWidth,
                message.ChatScrollSpeed,
                ct
            )
            .ConfigureAwait(false);
    }
}
