using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The variable is set". The client's form is a variable picker and nothing else: int param [0] is
/// the picker's source type and variable id [0] is the selection, so this asks whether the variable
/// holds a value for the target the box points at — not what that value is.
/// </summary>
/// <remarks>
/// The picker filters on <c>!alwaysAvailable</c>: the built-in variables that always exist (a
/// furni's own X, a user's index) are deliberately not offered here, because for them the answer
/// would be a constant yes.
/// </remarks>
[RoomObjectLogic("wf_cnd_has_var")]
public class WiredConditionHasVariable(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredVariableConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.HAS_VARIABLE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<WiredVariableTargetType>(
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.User,
                WiredVariableTargetType.Global,
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

    // Without this the client's picker opens empty: it lists the room's variables from the context
    // snapshot the server stamps onto the box, not from anything it knows on its own.
    public override List<WiredVariableContextSnapshot> GetWiredContextSnapshots() =>
        [
            new WiredVariableAllInRoomSnapshot()
            {
                ContextType = WiredContextType.AllVariablesInRoom,
                AllVariablesHash = _ctx.Furni.AllVariablesHash,
            },
        ];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool result = TryReadVariable(VariableIdAt(0), VariableTarget, out _);

        return IsNegative() ? !result : result;
    }
}
