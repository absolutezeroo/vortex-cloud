using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze team gate (the <c>es_gate_*</c> furni). Walking onto it joins that team (or leaves, if the
/// player is already on it) — the emulator's Freeze "choose a team" mechanic. One class claims all
/// four colour keys; the colour comes from the bound logic key's suffix via
/// <see cref="GameColorKey"/>. The gate is walkable and its state shows the team's member count
/// (kept in sync by <see cref="Systems.RoomFreezeSystem"/>). The count is live game display, so it
/// is never persisted.
/// </summary>
[RoomObjectLogic("freeze_gate_red")]
[RoomObjectLogic("freeze_gate_green")]
[RoomObjectLogic("freeze_gate_blue")]
[RoomObjectLogic("freeze_gate_yellow")]
public sealed class FurnitureFreezeGateLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public GameTeamColor TeamColor { get; } = GameColorKey.FromKeySuffix(ctx.Definition.LogicName);

    public override bool CanWalk() => true;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player)
        {
            await _ctx.Freeze.OnGateWalkOnAsync(player.PlayerId, TeamColor, ct);
        }
    }
}
