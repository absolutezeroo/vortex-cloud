using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.Rooms;

internal sealed partial class RoomService
{
    public async Task PlacePetInRoomAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    )
    {
        if (ctx.Origin != ActionOrigin.Player || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || petId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.PlacePetAsync(ctx, petId, x, y, direction, ct).ConfigureAwait(false);
    }

    public async Task MovePetInRoomAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    )
    {
        if (ctx.Origin != ActionOrigin.Player || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || petId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.MovePetAsync(ctx, petId, x, y, direction, ct).ConfigureAwait(false);
    }

    public async Task PickUpPetInRoomAsync(ActionContext ctx, int petId, CancellationToken ct)
    {
        if (ctx.Origin != ActionOrigin.Player || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || petId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.PickUpPetAsync(ctx, petId, ct).ConfigureAwait(false);
    }
}
