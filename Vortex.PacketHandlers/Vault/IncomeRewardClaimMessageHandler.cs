using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Vault;
using Vortex.Primitives.Messages.Outgoing.Vault;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Vault;

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
        {
            return;
        }

        bool success = await _grainFactory
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
