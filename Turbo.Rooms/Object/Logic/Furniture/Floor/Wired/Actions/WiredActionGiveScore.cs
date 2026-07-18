using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Awards points to each resolved user's own team (Habbo's "give points"). Int params from the
/// client setup form (actiontypes/GiveScore): [0] = points (signed; the "subtract" toggle stores a
/// negative), [1] = times-per-game cap (0 = unlimited). The cap is enforced per (this score box, user)
/// so the same user cannot be scored more than N times per game by this box. Delegates to
/// <see cref="Grains.Systems.RoomGameSystem"/>; users on no team are skipped.</summary>
[RoomObjectLogic("wf_act_give_score")]
public class WiredActionGiveScore(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.GIVE_SCORE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(-1000, 1000, 1), // points (signed)
            new WiredRangeParamRule(0, 10, 0), // times-per-game cap (0 = unlimited)
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        int points = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int cap = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;

        if (points == 0)
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            try
            {
                _roomGrain.GameSystem.TryGiveScoreToPlayerTeam(
                    _ctx.ObjectId,
                    playerId,
                    points,
                    cap
                );
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
