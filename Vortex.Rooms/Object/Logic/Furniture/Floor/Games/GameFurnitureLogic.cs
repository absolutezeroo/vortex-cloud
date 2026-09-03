using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

/// <summary>
/// The base every piece of arena furniture derives from. It carries the whole coupling between
/// furniture and games — the furni's identity, its tile, and the four raw events forwarded as
/// <see cref="GameSignal"/>s — so a concrete arena furni class is a logic key, a game id and a
/// capability interface, and contains no rules at all.
/// <para>
/// A game furni's state is live display, reset each match, so none of it is persisted: the state
/// on a Banzai patch or a Freeze block means something only inside the match that painted it.
/// </para>
/// <para>
/// Nothing here calls a game. The signal goes to the room's runtime, which routes it to whichever
/// module owns <see cref="Game"/> — which is why adding a game adds no member to the room object
/// context, and why a room with no games at all pays a null routing lookup and nothing more.
/// </para>
/// </summary>
public abstract class GameFurnitureLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx), IGameComponent
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    /// <summary>The game this furni belongs to. Constant for the instance.</summary>
    public abstract GameId Game { get; }

    public RoomObjectId ObjectId => _ctx.ObjectId;

    public int X => _ctx.RoomObject.X;

    public int Y => _ctx.RoomObject.Y;

    public override bool CanWalk() => true;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player)
        {
            await _ctx.Game.SignalAsync(GameSignal.WalkOn(this, player.PlayerId), ct);
        }
    }

    public override async Task OnWalkOffAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOffAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player)
        {
            await _ctx.Game.SignalAsync(GameSignal.WalkOff(this, player.PlayerId), ct);
        }
    }

    /// <summary>Deliberately does NOT advance the state the way ordinary furniture does: an arena
    /// furni's state is the game's, and letting a double-click cycle it would corrupt a live match.
    /// The use becomes an intent the game decides on — which is the whole of "the client sends
    /// intent, the server decides".</summary>
    public override Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct) =>
        _ctx.Game.SignalAsync(GameSignal.Use(this, ctx.PlayerId, param), ct);

    public override async Task OnDetachAsync(CancellationToken ct)
    {
        // Before the base publishes the room's own detach event, so a game that has to invalidate
        // its arena does it while the furni is still resolvable.
        await _ctx.Game.SignalAsync(GameSignal.Detached(this), ct);

        await base.OnDetachAsync(ct);
    }
}
