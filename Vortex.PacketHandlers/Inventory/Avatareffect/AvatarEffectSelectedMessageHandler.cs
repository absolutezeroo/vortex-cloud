using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Avatareffect;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Inventory.Avatareffect;

public class AvatarEffectSelectedMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<AvatarEffectSelectedMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        AvatarEffectSelectedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // effectType 0 means "stop wearing", so allow it; anything negative is invalid.
        if (ctx.PlayerId <= 0 || message.EffectType < 0)
        {
            return;
        }

        int applied = await _grainFactory
            .GetPlayerEffectGrain(ctx.PlayerId)
            .SelectEffectAsync(message.EffectType, ct)
            .ConfigureAwait(false);

        if (ctx.RoomId > 0)
        {
            await _grainFactory
                .GetRoomGrain(ctx.RoomId)
                .SetAvatarEffectAsync(ctx.AsActionContext(), applied, ct)
                .ConfigureAwait(false);
        }
    }
}
