using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// Reward boxes: one click, one prize, and the box is gone. Server-side this is a crackable whose
/// binding asks for a single hit, so it runs the same accumulate-then-pay path rather than a second
/// copy of it — the three names below differ only in the artwork and dialog the client puts on them.
///
/// A box bound to no pool stays inert instead of handing out free furniture, which is what makes
/// shipping the definitions ahead of their prizes safe.
/// </summary>
[RoomObjectLogic("furniture_ecotron_box")]
[RoomObjectLogic("furniture_nft_reward_box")]
[RoomObjectLogic("furniture_effectbox")]
public class FurnitureRewardBoxLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.Persistent;

    /// <summary>A box is opened by whoever the owner handed it to, so anyone may click it.</summary>
    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Everybody;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        await _ctx.RoomAs<IRoomCrackable>()
            .HitCrackableAsync(ctx, _ctx.ObjectId, ct)
            .ConfigureAwait(false);
    }
}
