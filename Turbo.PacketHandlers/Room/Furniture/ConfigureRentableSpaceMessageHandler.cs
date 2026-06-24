using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Furniture;
using Turbo.Primitives.Messages.Outgoing.Room.Furniture;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms.Snapshots;

namespace Turbo.PacketHandlers.Room.Furniture;

public class ConfigureRentableSpaceMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<ConfigureRentableSpaceMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IPermissionService _permissionService = permissionService;

    public async ValueTask HandleAsync(
        ConfigureRentableSpaceMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.FurnitureId <= 0)
        {
            return;
        }

        PermissionSet perms = await _permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        bool isStaff = perms.Has(Capabilities.Room.ModerateAny);

        bool success = await _grainFactory
            .GetRentableSpaceGrain(message.FurnitureId)
            .ConfigureAsync(
                ctx.PlayerId,
                isStaff,
                message.Price,
                message.CurrencyTypeId,
                message.RentDurationSeconds,
                message.RequiresHc,
                ct
            )
            .ConfigureAwait(false);

        if (!success)
        {
            return;
        }

        // Echo confirmed config back. Currencies are empty — the client already has them
        // from the initial GetConfig call and doesn't need them on the save echo.
        await ctx.SendComposerAsync(
                new RentableSpaceConfigMessageComposer
                {
                    FurnitureId = message.FurnitureId,
                    IsConfigured = true,
                    Price = message.Price,
                    CurrencyTypeId = message.CurrencyTypeId,
                    RentDurationSeconds = message.RentDurationSeconds,
                    RequiresHc = message.RequiresHc,
                    AvailableCurrencies = [],
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
