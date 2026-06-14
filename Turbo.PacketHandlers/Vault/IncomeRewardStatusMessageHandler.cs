using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Vault;
using Turbo.Primitives.Messages.Outgoing.Vault;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Vault;

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
            return;

        var rewards = await _grainFactory
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
