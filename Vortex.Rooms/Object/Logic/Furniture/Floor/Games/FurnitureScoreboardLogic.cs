using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

/// <summary>
/// A team scoreboard: the client's <c>furniture_score</c> logic shows the item's raw state value as
/// a number and animates the delta. One class serves every game's boards — <c>bb_score_*</c> bind to
/// <c>furniture_score</c> and carry their colour in the classname (<c>bb_score_r</c>), while
/// <c>es_score_*</c> bind to the Vortex <c>freeze_counter_*</c> keys which carry it in the key —
/// so the colour resolves from the logic key first and falls back to the classname. The displayed
/// score is pushed by <see cref="Grains.Systems.RoomGameScoreboardSystem"/> on every shared-score
/// change; it is live game display and never persisted.
/// </summary>
[RoomObjectLogic("furniture_score")]
[RoomObjectLogic("freeze_counter_red")]
[RoomObjectLogic("freeze_counter_green")]
[RoomObjectLogic("freeze_counter_blue")]
[RoomObjectLogic("freeze_counter_yellow")]
public sealed class FurnitureScoreboardLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public GameTeamColor TeamColor { get; } =
        GameColorKey.FromKeySuffix(ctx.Definition.LogicName) is var byKey
        && byKey != GameTeamColor.None
            ? byKey
            : GameColorKey.FromKeySuffix(ctx.Definition.Name);
}
