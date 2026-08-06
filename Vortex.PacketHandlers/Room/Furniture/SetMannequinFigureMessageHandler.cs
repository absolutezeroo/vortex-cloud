using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Saves the requester's current look onto a mannequin. The figure is deliberately read from the
/// player grain rather than taken from the packet: the client never sends one, and accepting one
/// would let anybody write an arbitrary look onto someone else's furniture.
/// </summary>
public class SetMannequinFigureMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetMannequinFigureMessage>
{
    public async ValueTask HandleAsync(
        SetMannequinFigureMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        PlayerSummarySnapshot player = await grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetSummaryAsync(ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .SetMannequinOutfitAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                player.Figure,
                player.Gender == AvatarGenderType.Female ? "f" : "m",
                ct
            )
            .ConfigureAwait(false);
    }
}
