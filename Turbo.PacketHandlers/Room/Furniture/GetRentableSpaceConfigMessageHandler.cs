using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Furniture;
using Turbo.Primitives.Messages.Outgoing.Room.Furniture;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Snapshots;

namespace Turbo.PacketHandlers.Room.Furniture;

public class GetRentableSpaceConfigMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetRentableSpaceConfigMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetRentableSpaceConfigMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.FurnitureId <= 0)
        {
            return;
        }

        RentableSpaceConfigSnapshot snapshot = await _grainFactory
            .GetRentableSpaceGrain(message.FurnitureId)
            .GetConfigAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new RentableSpaceConfigMessageComposer
                {
                    FurnitureId = snapshot.FurnitureId,
                    IsConfigured = snapshot.IsConfigured,
                    Price = snapshot.Price,
                    CurrencyTypeId = snapshot.CurrencyTypeId,
                    RentDurationSeconds = snapshot.RentDurationSeconds,
                    RequiresHc = snapshot.RequiresHc,
                    AvailableCurrencies = snapshot.AvailableCurrencies,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
