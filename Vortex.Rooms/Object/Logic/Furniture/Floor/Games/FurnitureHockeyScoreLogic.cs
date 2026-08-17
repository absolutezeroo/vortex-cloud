using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

/// <summary>
/// A hand-operated match counter (the client's <c>furniture_hockey_score</c> logic: <c>hockey_score</c>
/// and the <c>fball_score_*</c> boards). Deliberately NOT team-bound and NOT driven by the shared game
/// scores — on Habbo these are manual: the referee clicks the <c>inc</c>/<c>dec</c> sprite regions
/// (use params 2 / 1) and double-clicks <c>off</c> (param 3) to reset. The state is the raw score the
/// client displays. Controllers only, like the game timer; live display, never persisted.
/// <para>The param mapping (inc=2, dec=1, off=3) is read from the decompiled client logic — verify
/// once against a real client before trusting it in anger.</para>
/// </summary>
[RoomObjectLogic("furniture_hockey_score")]
public sealed class FurnitureHockeyScoreLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
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
