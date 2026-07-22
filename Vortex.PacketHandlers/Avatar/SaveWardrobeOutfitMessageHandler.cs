using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Avatar;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Avatar;

public class SaveWardrobeOutfitMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SaveWardrobeOutfitMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SaveWardrobeOutfitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .SaveWardrobeOutfitAsync(message.SlotId, message.Figure, message.Gender, ct)
            .ConfigureAwait(false);
    }
}
