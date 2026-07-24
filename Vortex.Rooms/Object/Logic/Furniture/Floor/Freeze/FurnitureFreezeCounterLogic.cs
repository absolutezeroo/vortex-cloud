using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze team scoreboard (the <c>es_score_*</c> furni). Its state shows that team's score, pushed by
/// <see cref="Systems.RoomFreezeSystem"/> (phase 4). Each colour is a concrete subclass carrying its
/// <see cref="GameTeamColor"/> so the system knows which team's score to display.
/// </summary>
public abstract class FurnitureFreezeCounterLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public abstract GameTeamColor TeamColor { get; }
}

[RoomObjectLogic("freeze_counter_red")]
public sealed class FurnitureFreezeCounterRedLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFreezeCounterLogic(stuffDataFactory, ctx)
{
    public override GameTeamColor TeamColor => GameTeamColor.Red;
}

[RoomObjectLogic("freeze_counter_green")]
public sealed class FurnitureFreezeCounterGreenLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFreezeCounterLogic(stuffDataFactory, ctx)
{
    public override GameTeamColor TeamColor => GameTeamColor.Green;
}

[RoomObjectLogic("freeze_counter_blue")]
public sealed class FurnitureFreezeCounterBlueLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFreezeCounterLogic(stuffDataFactory, ctx)
{
    public override GameTeamColor TeamColor => GameTeamColor.Blue;
}

[RoomObjectLogic("freeze_counter_yellow")]
public sealed class FurnitureFreezeCounterYellowLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFreezeCounterLogic(stuffDataFactory, ctx)
{
    public override GameTeamColor TeamColor => GameTeamColor.Yellow;
}
