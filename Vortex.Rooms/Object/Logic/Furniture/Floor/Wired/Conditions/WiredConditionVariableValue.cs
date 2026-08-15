using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The variable's value compares to ...". This is the box that makes wired variables worth
/// writing: until it existed a room could set variables that nothing could ever read back.
/// </summary>
/// <remarks>
/// Six int params, in the order the client pushes them: [0] the variable's source type, [1] the
/// comparison operator (the radio button's own id, not its position), [2] whether the right-hand
/// side is a literal (0) or another variable, [3] and [4] that literal as a signed long pair, and
/// [5] the reference variable's source type. Variable ids come in the matching order: the compared
/// variable first, the reference second.
/// <para>
/// One deliberate divergence: the client gives the reference variable its own input-source slot,
/// while the room resolves every slot of a box into a single selection. The reference is therefore
/// read against the same targets as the compared variable — indistinguishable in the ordinary case
/// (both sides pointed at the triggering user or the triggered furni), and only visible if someone
/// aims the two slots at different sources.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_cnd_var_val_match")]
public class WiredConditionVariableValue(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredVariableConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>The client's "value or variable" section sends 0 for a typed-in number.</summary>
    private const int LiteralOperand = 0;

    public override int WiredCode => (int)WiredConditionType.VARIABLE_VALUE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            TargetTypeRule(),
            new WiredEnumParamRule<WiredComparisonType>(
                WiredComparisonType.Equals,
                WiredComparisonType.LessThan,
                WiredComparisonType.Equals,
                WiredComparisonType.GreaterThan,
                WiredComparisonType.LessThanOrEquals,
                WiredComparisonType.NotEquals,
                WiredComparisonType.GreaterTHanOrEquals
            ),
            new WiredParamRule(LiteralOperand),
            // The two halves of Util.pushIntAsLong: any int is a legal half, so neither is range
            // checked -- rejecting one would make the whole box unsaveable.
            new WiredParamRule(0),
            new WiredParamRule(0),
            TargetTypeRule(),
        ];

    public override int GetMaxVariableIds() => 2;

    // Two slots, matching the client's two merged selections: the compared variable and the
    // reference one.
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [.. FurniSources],
            [.. FurniSources],
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [.. PlayerSources],
            [.. PlayerSources],
        ];

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
        bool result = false;

        if (
            _wiredData.IntParams.Count >= 6
            && TryReadVariable(VariableIdAt(0), VariableTarget, out WiredVariableValue left)
            && TryResolveOperand(out int right)
        )
        {
            result = WiredVariableComparison.Matches(
                left.Value,
                _wiredData.GetIntParam<WiredComparisonType>(1),
                right
            );
        }

        return IsNegative() ? !result : result;
    }

    /// <summary>The right-hand side: either the number typed into the form or the value of a second
    /// variable. A reference variable that holds nothing makes the comparison fail rather than
    /// compare against a stand-in zero.</summary>
    private bool TryResolveOperand(out int operand)
    {
        if (_wiredData.GetIntParam<int>(2) == LiteralOperand)
        {
            operand = WiredIntAsLong.ReadClamped(
                _wiredData.GetIntParam<int>(3),
                _wiredData.GetIntParam<int>(4)
            );

            return true;
        }

        bool found = TryReadVariable(
            VariableIdAt(1),
            _wiredData.GetIntParam<WiredVariableTargetType>(5),
            out WiredVariableValue value
        );

        operand = found ? value.Value : 0;

        return found;
    }

    private static WiredEnumParamRule<WiredVariableTargetType> TargetTypeRule() =>
        new(
            WiredVariableTargetType.Furni,
            WiredVariableTargetType.Furni,
            WiredVariableTargetType.User,
            WiredVariableTargetType.Global,
            WiredVariableTargetType.Context
        );

    private static readonly WiredFurniSourceType[] FurniSources =
    [
        WiredFurniSourceType.SelectedItems,
        WiredFurniSourceType.SelectorItems,
        WiredFurniSourceType.SignalItems,
        WiredFurniSourceType.TriggeredItem,
    ];

    private static readonly WiredPlayerSourceType[] PlayerSources =
    [
        WiredPlayerSourceType.TriggeredUser,
        WiredPlayerSourceType.SelectorUsers,
        WiredPlayerSourceType.SignalUsers,
    ];
}
