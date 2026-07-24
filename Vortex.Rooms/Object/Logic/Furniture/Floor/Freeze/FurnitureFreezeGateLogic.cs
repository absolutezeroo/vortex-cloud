using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze team gate (the <c>es_gate_*</c> furni). Walking onto it joins that team (or leaves, if the
/// player is already on it) — the emulator's Freeze "choose a team" mechanic. Each colour is a concrete
/// subclass carrying its <see cref="GameTeamColor"/>; the gate is walkable and its state shows the
/// team's member count (kept in sync by <see cref="Systems.RoomFreezeSystem"/>).
/// </summary>
public abstract class FurnitureFreezeGateLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public abstract GameTeamColor TeamColor { get; }

    public override bool CanWalk() => true;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player)
        {
            await _roomGrain.FreezeSystem.OnGateWalkOnAsync(player.PlayerId, TeamColor, ct);
        }
    }
}

[RoomObjectLogic("freeze_gate_red")]
public sealed class FurnitureFreezeGateRedLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFreezeGateLogic(stuffDataFactory, ctx)
{
    public override GameTeamColor TeamColor => GameTeamColor.Red;
}

[RoomObjectLogic("freeze_gate_green")]
public sealed class FurnitureFreezeGateGreenLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFreezeGateLogic(stuffDataFactory, ctx)
{
    public override GameTeamColor TeamColor => GameTeamColor.Green;
}

[RoomObjectLogic("freeze_gate_blue")]
public sealed class FurnitureFreezeGateBlueLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFreezeGateLogic(stuffDataFactory, ctx)
{
    public override GameTeamColor TeamColor => GameTeamColor.Blue;
}

[RoomObjectLogic("freeze_gate_yellow")]
public sealed class FurnitureFreezeGateYellowLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFreezeGateLogic(stuffDataFactory, ctx)
{
    public override GameTeamColor TeamColor => GameTeamColor.Yellow;
}
