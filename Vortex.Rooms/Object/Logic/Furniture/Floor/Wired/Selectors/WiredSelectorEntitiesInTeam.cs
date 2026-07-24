using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

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
            // None is the client's default option ("Any team" / "Triggerer's team"); rejecting it
            // makes the box impossible to save straight out of the catalogue.
            new WiredEnumParamRule<GameTeamColor>(
                GameTeamColor.None,
                GameTeamColor.None,
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
            GameTeamColor required = _wiredData.GetIntParam<GameTeamColor>(0);

            // None is the client's "Any team" option: it means everyone who is on a team, not the
            // members of a "none" team (which is simply everyone unassigned).
            GameTeamColor[] wanted =
                required == GameTeamColor.None
                    ?
                    [
                        GameTeamColor.Red,
                        GameTeamColor.Green,
                        GameTeamColor.Blue,
                        GameTeamColor.Yellow,
                    ]
                    : [required];

            foreach (GameTeamColor color in wanted)
            {
                foreach (PlayerId playerId in _roomGrain.GameSystem.GetPlayersInTeam(color))
                {
                    output.SelectedPlayerIds.Add(playerId);
                }
            }
        }

        return Task.FromResult<IWiredSelectionSet>(output);
    }
}
