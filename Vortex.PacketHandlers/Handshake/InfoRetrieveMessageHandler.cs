using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Handshake;

public class InfoRetrieveMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<InfoRetrieveMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        InfoRetrieveMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        IPlayerGrain player = _grainFactory.GetPlayerGrain(ctx.PlayerId);
        PlayerSummarySnapshot snapshot = await player.GetSummaryAsync(ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(new UserObjectMessage { Player = snapshot }, ct)
            .ConfigureAwait(false);
    }
}
