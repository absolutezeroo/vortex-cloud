using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots;

namespace Vortex.PacketHandlers.Room.Furniture;

public class RentableSpaceStatusMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RentableSpaceStatusMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RentableSpaceStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.FurnitureId <= 0)
        {
            return;
        }

        RentableSpaceStatusSnapshot snapshot = await _grainFactory
            .GetRentableSpaceGrain(message.FurnitureId)
            .GetStatusAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new RentableSpaceStatusMessageComposer
                {
                    Rented = snapshot.Rented,
                    CanRentErrorCode = snapshot.CanRentErrorCode,
                    RenterId = snapshot.RenterId,
                    RenterName = snapshot.RenterName,
                    TimeRemaining = snapshot.TimeRemaining,
                    Price = snapshot.Price,
                    CurrencyName = snapshot.CurrencyName,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
