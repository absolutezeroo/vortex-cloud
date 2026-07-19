using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Register;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.PacketHandlers.Register;

public class UpdateFigureDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpdateFigureDataMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UpdateFigureDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId < 0)
        {
            return;
        }

        IPlayerGrain player = _grainFactory.GetPlayerGrain(ctx.PlayerId);

        await player
            .SetFigureAsync(
                message.Figure,
                AvatarGenderTypeExtensions.FromLegacyString(message.Gender),
                ct
            )
            .ConfigureAwait(false);
    }
}
