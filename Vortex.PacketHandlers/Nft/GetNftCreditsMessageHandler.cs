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

public class GetNftCreditsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetNftCreditsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetNftCreditsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain(ctx.PlayerId);
        int emeralds = await wallet
            .GetAmountForCurrencyAsync(
                new CurrencyKind { CurrencyType = CurrencyType.Emeralds },
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new EmeraldBalanceMessageComposer { EmeraldBalance = emeralds },
                ct
            )
            .ConfigureAwait(false);
    }
}
