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

[RoomObjectLogic("wf_act_give_var")]
public class WiredActionGiveVariable(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.GIVE_VARIABLE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<WiredVariableTargetType>(
                WiredVariableTargetType.User,
                WiredVariableTargetType.User,
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.Context
            ),
            new WiredRangeParamRule(0, 0, 0),
            new WiredParamRule(0), // init value
            new WiredBoolParamRule(false), // override
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

                int value = _wiredData.GetIntParam<int>(2);
                bool replace = _wiredData.GetIntParam<bool>(3);

                switch (_wiredData.GetIntParam<WiredVariableTargetType>(0))
                {
                    case WiredVariableTargetType.Furni:
                    {
                        foreach (int furniId in selection.SelectedFurniIds)
                        {
                            WiredVariableKey key = new WiredVariableKey(
                                id,
                                WiredVariableTargetType.Furni,
                                furniId
                            );

                            await variable.GiveValueAsync(key, value, replace);
                        }

                        break;
                    }
                    case WiredVariableTargetType.User:
                    {
                        foreach (int playerId in selection.SelectedPlayerIds)
                        {
                            WiredVariableKey key = new WiredVariableKey(
                                id,
                                WiredVariableTargetType.User,
                                playerId
                            );

                            await variable.GiveValueAsync(key, value, replace);
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
