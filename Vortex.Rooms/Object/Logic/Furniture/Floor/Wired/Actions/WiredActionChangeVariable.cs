using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// "Change variable": the arithmetic half of wired variables. Give and remove could only put a
/// fixed value in place or take it away, so a variable could never accumulate — no score that adds
/// up, no counter that decrements, no level that doubles.
/// </summary>
/// <remarks>
/// Same six int params as the value condition, in the client's order: [0] the variable's source
/// type, [1] the operation, [2] whether the operand is a literal or a second variable, [3] and [4]
/// that literal as a signed long pair, [5] the reference variable's source type. Variable ids come
/// as the changed variable then the reference.
/// <para>
/// The operation is read as a plain int rather than through an enum rule on purpose: the client's
/// dropdown offers ids this revision does not name (111-118), and rejecting them would make the box
/// impossible to save instead of merely inert.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_act_change_var_val")]
public class WiredActionChangeVariable(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>The client's "value or variable" section sends 0 for a typed-in number.</summary>
    private const int LiteralOperand = 0;

    public override int WiredCode => (int)WiredActionType.CHANGE_VARIABLE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            TargetTypeRule(),
            new WiredParamRule((int)WiredVariableOperation.Assign),
            new WiredParamRule(LiteralOperand),
            new WiredParamRule(0),
            new WiredParamRule(0),
            TargetTypeRule(),
        ];

    public override int GetMaxVariableIds() => 2;

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

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        if (_wiredData.IntParams.Count < 6)
        {
            return false;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);
        WiredVariableTargetType target = _wiredData.GetIntParam<WiredVariableTargetType>(0);

        if (
            !WiredVariableAccess.TryResolve(
                _ctx.Furni,
                VariableIdAt(0),
                out WiredVariableId id,
                out IWiredVariable? variable
            )
        )
        {
            return false;
        }

        WiredVariableOperation operation = (WiredVariableOperation)_wiredData.GetIntParam<int>(1);

        if (!TryResolveOperand(operation, selection, out int operand))
        {
            return false;
        }

        foreach (int targetId in WiredVariableAccess.TargetIds(target, selection))
        {
            WiredVariableKey key = new WiredVariableKey(id, target, targetId);

            // An unwritten variable starts the arithmetic at zero rather than at the store's
            // Default (which is 1) -- "add 5" to something untouched must give 5, not 6.
            int current = variable!.TryGetValue(key, out WiredVariableValue existing)
                ? existing.Value
                : 0;

            if (
                !WiredVariableArithmetic.TryApply(
                    current,
                    operation,
                    operand,
                    Random.Shared,
                    out int updated
                )
            )
            {
                _logger.LogDebug(
                    "Wired change-variable on item {ItemId} left {Key} alone: operation {Operation} does not apply.",
                    _ctx.ObjectId,
                    key.ToStorageKey(),
                    (int)operation
                );

                continue;
            }

            // SetValue only updates a key that exists (and is the path a variable can intercept);
            // a first write has to go through Give, or the very first "add" would vanish.
            if (!await variable.SetValueAsync(ctx, key, updated))
            {
                await variable.GiveValueAsync(key, updated, replace: true);
            }
        }

        return true;
    }

    private string VariableIdAt(int index) =>
        _wiredData.VariableIds.Count > index ? _wiredData.VariableIds[index] : string.Empty;

    /// <summary>The operand: the number typed into the form, the value of a second variable, or
    /// nothing at all for the three unary operations, whose form hides the field.</summary>
    private bool TryResolveOperand(
        WiredVariableOperation operation,
        IWiredSelectionSet selection,
        out int operand
    )
    {
        operand = 0;

        if (!WiredVariableArithmetic.RequiresOperand(operation))
        {
            return true;
        }

        if (_wiredData.GetIntParam<int>(2) == LiteralOperand)
        {
            operand = WiredIntAsLong.ReadClamped(
                _wiredData.GetIntParam<int>(3),
                _wiredData.GetIntParam<int>(4)
            );

            return true;
        }

        bool found = WiredVariableAccess.TryRead(
            _ctx.Furni,
            VariableIdAt(1),
            _wiredData.GetIntParam<WiredVariableTargetType>(5),
            selection,
            out WiredVariableValue value
        );

        operand = found ? value.Value : 0;

        // A reference that holds nothing writes nothing: falling back to zero would quietly assign
        // or add the wrong number.
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
