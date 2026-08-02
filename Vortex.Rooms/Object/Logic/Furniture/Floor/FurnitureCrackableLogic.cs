using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// Furniture that takes several hits before it pays out — eggs, piñatas, the event tents. Anyone may
/// hit one: the whole point is that a room cracks it together, and the room's default
/// controller-only usage policy would leave visitors clicking a furniture that never responds.
///
/// The counters are server state. The client's crackable logic only copies what it is sent into the
/// infostand's "hits remaining", so nothing here can be decided by the clicker.
/// </summary>
[RoomObjectLogic("furniture_crackable")]
public class FurnitureCrackableLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.Persistent;

    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Everybody;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        await _ctx.Room.HitCrackableAsync(ctx, _ctx.ObjectId, ct).ConfigureAwait(false);
    }
}
