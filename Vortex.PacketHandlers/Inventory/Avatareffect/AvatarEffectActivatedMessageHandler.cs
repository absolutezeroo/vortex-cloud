using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Avatareffect;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Inventory.Avatareffect;

public class AvatarEffectActivatedMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<AvatarEffectActivatedMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        AvatarEffectActivatedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.EffectType <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerEffectGrain(ctx.PlayerId)
            .ActivateEffectAsync(message.EffectType, ct)
            .ConfigureAwait(false);
    }
}
