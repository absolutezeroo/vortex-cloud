using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Banzai;

/// <summary>
/// A Battle Banzai team gate (<c>bb_gate_*</c>). Walking onto it joins that team (or leaves, if the
/// player is already on it) while the game is idle; during a round the gate is physically
/// unwalkable (Arcturus behaviour — walkability is precomputed into the tile flags, so
/// <c>RoomBanzaiSystem</c> recomputes each gate's tile when the phase flips). One class claims all
/// four colour keys via <see cref="GameColorKey"/>; the state shows the team's member count, live
/// display only.
/// </summary>
[RoomObjectLogic("battlebanzai_gate_red")]
[RoomObjectLogic("battlebanzai_gate_green")]
[RoomObjectLogic("battlebanzai_gate_blue")]
[RoomObjectLogic("battlebanzai_gate_yellow")]
public sealed class FurnitureBanzaiGateLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public GameTeamColor TeamColor { get; } = GameColorKey.FromKeySuffix(ctx.Definition.LogicName);

    public override bool CanWalk() => !_ctx.Banzai.IsRoundRunning;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player)
        {
            await _ctx.Banzai.OnGateWalkOnAsync(player.PlayerId, TeamColor, ct);
        }
    }
}
