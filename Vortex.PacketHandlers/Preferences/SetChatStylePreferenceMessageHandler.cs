using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Protocol.Messages.Incoming.Preferences;

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

        // Orchestration-only: the grain owns the persistence (and no-ops when unchanged). Both
        // halves are persisted — the client reads the font size back off the account-preferences
        // packet (HabboFreeFlowChat::onAccountPreferences), so one we do not store is one the
        // player re-picks after every login.
        IPlayerGrain player = _grainFactory.GetPlayerGrain(ctx.PlayerId);

        await player.SetChatStylePreferenceAsync(message.ChatStyle, ct).ConfigureAwait(false);
        await player.SetChatSizePreferenceAsync(message.FontSizeMode, ct).ConfigureAwait(false);
    }
}
