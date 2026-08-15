using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// "Filter to X furni" / "filter to X users": caps what the selectors handed the stack, so an action
/// that would have run on everything the selectors found runs on a few of them instead.
/// </summary>
/// <remarks>
/// This was previously modelled as a selector, and reported itself to the client as
/// <c>FURNI_WITH_VARIABLE</c> — a code already taken by the "furni with variable" selector. The box
/// is an add-on on the client's side (<c>SelectorFilter.as</c>, add-on codes 10 and 11), so opening
/// one drew the wrong dialog entirely.
/// <para>
/// Int params are the value-or-variable section, unencoded: [0] the amount (1-1000), [1] whether
/// that amount is a literal or comes from a variable, [2] that variable's source type. Note this
/// section sends its number as a plain int, unlike the variable comparison boxes which push theirs
/// as a long pair.
/// </para>
/// <para>
/// Which ones survive is not something the form lets a player choose — there is no ordering
/// criterion on this box, unlike its "highest/lowest variable" siblings — so the kept set is drawn
/// at random.
/// </para>
/// </remarks>
public abstract class WiredAddonSelectorFilter(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>The client's value-or-variable section sends 0 when the amount was typed in.</summary>
    private const int LiteralAmount = 0;

    private const int MinAmount = 1;

    private const int MaxAmount = 1000;

    /// <summary>Whether this furni trims the furni side of the pool or the user side.</summary>
    protected abstract bool FiltersFurni { get; }

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(MinAmount, MaxAmount, MinAmount),
            new WiredParamRule(LiteralAmount),
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

    // The amount can be read from a variable, so the box needs the room's variable list or its
    // picker opens empty.
    public override List<WiredVariableContextSnapshot> GetWiredContextSnapshots() =>
        [
            new WiredVariableAllInRoomSnapshot()
            {
                ContextType = WiredContextType.AllVariablesInRoom,
                AllVariablesHash = _ctx.Furni.AllVariablesHash,
            },
        ];

    public override async Task<bool> MutatePolicyAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        int? amount = await ResolveAmountAsync(ctx, ct);

        if (amount is not int keep || keep < MinAmount)
        {
            return true;
        }

        HashSet<int> pool = FiltersFurni
            ? ctx.SelectorPool.SelectedFurniIds
            : ctx.SelectorPool.SelectedPlayerIds;

        if (pool.Count <= keep)
        {
            return true;
        }

        int[] kept = [.. pool.OrderBy(_ => Random.Shared.Next()).Take(keep)];

        pool.Clear();
        pool.UnionWith(kept);

        return true;
    }

    /// <summary>The configured amount: the number on the form, or the value of the variable it was
    /// pointed at instead. Null when a variable was chosen and holds nothing — filtering to an
    /// unknown amount would silently empty the pool.</summary>
    private async Task<int?> ResolveAmountAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (_wiredData.IntParams.Count < 3)
        {
            return null;
        }

        if (_wiredData.GetIntParam<int>(1) == LiteralAmount)
        {
            return _wiredData.GetIntParam<int>(0);
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        return WiredVariableAccess.TryRead(
            _ctx.Furni,
            _wiredData.VariableIds.Count > 0 ? _wiredData.VariableIds[0] : string.Empty,
            _wiredData.GetIntParam<WiredVariableTargetType>(2),
            selection,
            out WiredVariableValue value
        )
            ? value.Value
            : null;
    }
}
