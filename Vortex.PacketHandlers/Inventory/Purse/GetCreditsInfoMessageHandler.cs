using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory;
using Vortex.Primitives.Messages.Incoming.Inventory.Purse;
using Vortex.Primitives.Messages.Outgoing.Inventory.Purse;
using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.PacketHandlers.Inventory.Purse;

public class GetCreditsInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetCreditsInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetCreditsInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain(ctx.PlayerId);
        int credits = await wallet
            .GetAmountForCurrencyAsync(new CurrencyKind { CurrencyType = CurrencyType.Credits }, ct)
            .ConfigureAwait(false);
        Dictionary<int, int> activityPoints = await wallet
            .GetActivityPointsAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CreditBalanceEventMessageComposer { Balance = $"{credits}.0" },
                ct
            )
            .ConfigureAwait(false);
        await ctx.SendComposerAsync(
                new ActivityPointsMessageComposer { PointsByCategoryId = activityPoints },
                ct
            )
            .ConfigureAwait(false);
    }
}
