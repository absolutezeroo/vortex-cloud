using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums.Games;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

/// <summary>Selects every player currently on the configured team (Habbo's "users in team" input
/// source). Int param [0] is the team (1-4, matching <see cref="GameTeamColor"/>). Reads the single
/// source of truth in <see cref="Grains.Systems.RoomGameSystem"/>.</summary>
[RoomObjectLogic("wf_slc_users_team")]
public class WiredSelectorEntitiesInTeam(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_IN_TEAM;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<GameTeamColor>(
                GameTeamColor.Red,
                GameTeamColor.Red,
                GameTeamColor.Green,
                GameTeamColor.Blue,
                GameTeamColor.Yellow
            ),
        ];

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        WiredSelectionSet output = new WiredSelectionSet();

        if (_wiredData.IntParams.Count > 0)
        {
            foreach (
                PlayerId playerId in _roomGrain.GameSystem.GetPlayersInTeam(
                    _wiredData.GetIntParam<GameTeamColor>(0)
                )
            )
            {
                output.SelectedPlayerIds.Add(playerId);
            }
        }

        return Task.FromResult<IWiredSelectionSet>(output);
    }
}
