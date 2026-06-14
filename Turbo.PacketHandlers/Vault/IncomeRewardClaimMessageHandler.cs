using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Vault;
using Turbo.Primitives.Messages.Outgoing.Vault;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Vault;

public class IncomeRewardClaimMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<IncomeRewardClaimMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        IncomeRewardClaimMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        var success = await _grainFactory
            .GetPlayerVaultGrain(ctx.PlayerId)
            .ClaimCategoryAsync(message.Category, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new IncomeRewardClaimResponseMessageComposer
                {
                    RewardCategory = message.Category,
                    Result = success,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
