using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Nft;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.PacketHandlers.Nft;

public class GetSilverMessageHandler(IGrainFactory grainFactory) : IMessageHandler<GetSilverMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetSilverMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain(ctx.PlayerId);
        int silver = await wallet
            .GetAmountForCurrencyAsync(new CurrencyKind { CurrencyType = CurrencyType.Silver }, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new SilverBalanceMessageComposer { SilverBalance = silver }, ct)
            .ConfigureAwait(false);
    }
}
