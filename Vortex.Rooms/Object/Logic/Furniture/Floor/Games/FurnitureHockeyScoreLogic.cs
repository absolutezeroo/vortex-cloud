using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

/// <summary>
/// A match counter (the client's <c>furniture_hockey_score</c> logic: <c>hockey_score</c> and the
/// <c>fball_score_*</c> boards). It is driven two ways, which is what the reference emulator's
/// <c>InteractionFootballScoreboard</c> does as well:
/// <list type="bullet">
/// <item><b>By hand.</b> The referee clicks the <c>inc</c>/<c>dec</c> sprite regions (use params 2 /
/// 1) and double-clicks <c>off</c> (param 3) to reset.</item>
/// <item><b>By the game.</b> It is an <see cref="IScoreDisplayComponent"/>, so the scoreboard
/// presenter paints it from the shared team scores like every other board. This is the ONLY way a
/// football score reaches the room: football has no gates and no participants, and a goal credits
/// the colour the net wears — so a <c>fball_score_r</c> is what a red goal is for.</item>
/// </list>
/// <para>
/// The colour comes from the classname (<c>fball_score_r</c>), because the logic key carries none —
/// a plain <c>hockey_score</c> therefore resolves to <see cref="GameTeamColor.None"/>, matches no
/// team, and stays hand-operated, which is right: it is a generic counter.
/// </para>
/// <para>
/// The param mapping is <b>verified against the WIN63 client</b>
/// (<c>FurnitureHockeyScoreLogic.as</c>): a click on the <c>inc</c> sprite dispatches state 2, on
/// <c>dec</c> state 1, a double-click on <c>off</c> state 3 — and <c>useObject()</c>, which is what
/// an ordinary double-click anywhere on the furni reaches, also sends 3. The client carries them in
/// <c>UseFurniture</c> (header 3353), so <c>inc</c> and <c>dec</c> only ever fire when the furni's
/// visualization actually tags those sprites; a plain double-click does not depend on a tag.
/// </para>
/// </summary>
[RoomObjectLogic("furniture_hockey_score")]
public sealed class FurnitureHockeyScoreLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx), IScoreDisplayComponent
{
    public GameTeamColor Team { get; } =
        GameColorKey.FromKeySuffix(ctx.Definition.LogicName) is var byKey
        && byKey != GameTeamColor.None
            ? byKey
            : GameColorKey.FromKeySuffix(ctx.Definition.Name);

    private const int ParamDecrement = 1;
    private const int ParamIncrement = 2;
    private const int ParamReset = 3;

    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Controller;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        int score = GetState();

        switch (param)
        {
            case ParamIncrement:
                await SetStateAsync(score + 1);
                break;
            case ParamDecrement when score > 0:
                await SetStateAsync(score - 1);
                break;
            case ParamReset:
                await SetStateAsync(0);
                break;
        }
    }
}
