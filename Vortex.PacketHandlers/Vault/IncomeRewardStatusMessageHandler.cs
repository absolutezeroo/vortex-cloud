using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Vault;
using Vortex.Primitives.Messages.Outgoing.Vault;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Vault;

namespace Vortex.PacketHandlers.Vault;

public class IncomeRewardStatusMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<IncomeRewardStatusMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        IncomeRewardStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        List<IncomeRewardSnapshot> rewards = await _grainFactory
            .GetPlayerVaultGrain(ctx.PlayerId)
            .GetIncomeRewardsAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new IncomeRewardStatusMessageComposer { IncomeRewards = rewards },
                ct
            )
            .ConfigureAwait(false);
    }
}
