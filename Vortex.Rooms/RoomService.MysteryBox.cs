using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms;

internal sealed partial class RoomService
{
    public async Task CancelMysteryBoxWaitAsync(
        ActionContext ctx,
        PlayerId boxOwnerId,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || boxOwnerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .CancelMysteryBoxWaitAsync(ctx, boxOwnerId, ct)
            .ConfigureAwait(false);
    }

    public async Task OpenMysteryTrophyAsync(
        ActionContext ctx,
        RoomObjectId objectId,
        string inscription,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || objectId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .OpenMysteryTrophyAsync(ctx, objectId, inscription, ct)
            .ConfigureAwait(false);
    }
}
