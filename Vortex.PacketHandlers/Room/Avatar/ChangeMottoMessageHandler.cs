using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Room.Avatar;

public class ChangeMottoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ChangeMottoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ChangeMottoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId < 0)
        {
            return;
        }

        IPlayerGrain player = _grainFactory.GetPlayerGrain(ctx.PlayerId);

        await player.SetMottoAsync(message.Text, ct).ConfigureAwait(false);
    }
}
