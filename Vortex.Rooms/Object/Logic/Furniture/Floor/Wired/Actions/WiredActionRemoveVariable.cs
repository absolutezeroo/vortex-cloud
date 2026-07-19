using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Clears the selected variable's stored value for each resolved target (Habbo's "remove
/// variable"). The client setup form (actiontypes/class_3839) exposes a single variable picker plus a
/// source-type selector, so int param [0] is the <see cref="WiredVariableTargetType"/> (User / Furni /
/// Context) and exactly one variable id is configured. Mirrors <see cref="WiredActionGiveVariable"/>,
/// swapping the give for a per-key <see cref="IWiredVariableStore.RemoveValue"/>.</summary>
[RoomObjectLogic("wf_act_remove_var")]
public class WiredActionRemoveVariable(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.REMOVE_VARIABLE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<WiredVariableTargetType>(
                WiredVariableTargetType.User,
                WiredVariableTargetType.User,
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.Context
            ),
        ];

    public override int GetMaxVariableIds() => 1;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override List<WiredVariableContextSnapshot> GetWiredContextSnapshots() =>
        [
            new WiredVariableAllInRoomSnapshot()
            {
                ContextType = WiredContextType.AllVariablesInRoom,
                AllVariablesHash = _roomGrain._state.AllVariablesHash,
            },
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);
        List<string> variableIds = _wiredData.VariableIds;

        foreach (string variableId in variableIds)
        {
            try
            {
                WiredVariableId id = WiredVariableId.Parse(variableId);
                IWiredVariable? variable = _roomGrain.WiredSystem.GetVariableById(id);

                if (variable is null)
                {
                    continue;
                }

                switch (_wiredData.GetIntParam<WiredVariableTargetType>(0))
                {
                    case WiredVariableTargetType.Furni:
                    {
                        foreach (int furniId in selection.SelectedFurniIds)
                        {
                            variable.RemoveValue(
                                new WiredVariableKey(id, WiredVariableTargetType.Furni, furniId)
                            );
                        }

                        break;
                    }
                    case WiredVariableTargetType.User:
                    {
                        foreach (int playerId in selection.SelectedPlayerIds)
                        {
                            variable.RemoveValue(
                                new WiredVariableKey(id, WiredVariableTargetType.User, playerId)
                            );
                        }

                        break;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
