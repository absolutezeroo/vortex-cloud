using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Furniture;
using Turbo.Primitives.Messages.Outgoing.Room.Furniture;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Snapshots;

namespace Turbo.PacketHandlers.Room.Furniture;

public class RentableSpaceCancelRentMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<RentableSpaceCancelRentMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IPermissionService _permissionService = permissionService;

    public async ValueTask HandleAsync(
        RentableSpaceCancelRentMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.FurnitureId <= 0)
        {
            return;
        }

        // Staff check: hasSecurity(5) = room.moderate.any capability (DATA-MODEL §3.4).
        PermissionSet perms = await _permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        bool isStaff = perms.Has(Capabilities.Room.ModerateAny);

        bool succeeded = await _grainFactory
            .GetRentableSpaceGrain(message.FurnitureId)
            .CancelRentAsync(ctx.PlayerId, isStaff, ct)
            .ConfigureAwait(false);

        if (!succeeded)
        {
            await ctx.SendComposerAsync(
                    new RentableSpaceRentFailedMessageComposer
                    {
                        Reason = RentableSpaceRentFailedType.NotRentedByYou,
                    },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        // Send updated (free) status back to the actor.
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
