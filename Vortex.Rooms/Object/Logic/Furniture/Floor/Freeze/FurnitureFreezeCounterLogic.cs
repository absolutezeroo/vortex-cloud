using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze team scoreboard (the <c>es_score_*</c> furni, client logic <c>furniture_score</c>: it shows
/// its raw state value as a number). That state is the team's live score, pushed by
/// <see cref="Systems.RoomFreezeSystem"/>. One class claims all four colour keys; the colour comes
/// from the bound logic key's suffix via <see cref="GameColorKey"/>. The score is game display, so
/// it is never persisted.
/// </summary>
[RoomObjectLogic("freeze_counter_red")]
[RoomObjectLogic("freeze_counter_green")]
[RoomObjectLogic("freeze_counter_blue")]
[RoomObjectLogic("freeze_counter_yellow")]
public sealed class FurnitureFreezeCounterLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public GameTeamColor TeamColor { get; } = GameColorKey.FromKeySuffix(ctx.Definition.LogicName);
}
