using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetRoomStatsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetRoomStatsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetRoomStatsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredRoomStatsSnapshot stats = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetWiredRoomStatsAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new WiredRoomStatsEventMessageComposer
                {
                    ExecutionCost = stats.ExecutionCost,
                    ExecutionCostCap = stats.ExecutionCostCap,
                    IsHeavy = stats.IsHeavy,
                    FloorItemCount = stats.FloorItemCount,
                    FloorItemCap = stats.FloorItemCap,
                    WallItemCount = stats.WallItemCount,
                    WallItemCap = stats.WallItemCap,
                    PermanentFurniVariables = stats.PermanentFurniVariables,
                    MaxPermanentFurniVariables = stats.MaxPermanentFurniVariables,
                    PermanentUserVariables = stats.PermanentUserVariables,
                    MaxPermanentUserVariables = stats.MaxPermanentUserVariables,
                    PermanentGlobalVariables = stats.PermanentGlobalVariables,
                    MaxPermanentGlobalVariables = stats.MaxPermanentGlobalVariables,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
