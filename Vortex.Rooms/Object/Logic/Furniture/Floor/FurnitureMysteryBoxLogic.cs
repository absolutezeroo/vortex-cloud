using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A mystery box needs two people, so anyone may click it -- the key holder is by definition not the
/// owner, and the room's default controller-only usage policy would keep them out. Picking the box
/// up drops any pairing waiting on it.
///
/// The box's colour is its own furniture state (see <see cref="MysteryBoxSprite"/>): the asset has
/// no recolour data, only 25 states, so a box left at state 0 renders colourless whatever it was
/// meant to be. The state is set when the box is created and travels with the instance, which is
/// what lets one definition cover all eight colours.
/// </summary>
[RoomObjectLogic("furniture_mysterybox")]
public class FurnitureMysteryBoxLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Everybody;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        await _ctx.Room.UseMysteryBoxAsync(ctx, _ctx.ObjectId, ct).ConfigureAwait(false);
    }
}
