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
/// "Furni / Users with Highest or Lowest Variable" — trims the pool the selectors filled down to
/// the N that rank best by a variable.
/// </summary>
/// <remarks>
/// The ranked-by side is what separates these from the plain "filter to X" add-on, which keeps an
/// arbitrary N. Int params are <c>[amount, sortMode, literal-or-variable, that variable's source
/// type]</c>, and the variable ids are the one to rank by then the one the amount may come from.
/// <para>
/// The variable being ranked by is targeted at whatever this box filters — furni for one, users for
/// the other — so its target type is not sent: the box is the answer.
/// </para>
/// </remarks>
public abstract class WiredAddonVariableSortFilter(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int LiteralAmount = 0;

    private const int MinAmount = 1;

    private const int MaxAmount = 1000;

    /// <summary>Whether this furni trims the furni side of the pool or the user side, which is also
    /// the target the ranking variable is stored against.</summary>
    protected abstract bool FiltersFurni { get; }

    private WiredVariableTargetType RankedTarget =>
        FiltersFurni ? WiredVariableTargetType.Furni : WiredVariableTargetType.User;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(MinAmount, MaxAmount, MinAmount),
            new WiredEnumParamRule<WiredVariableSort>(WiredVariableSort.HighestValue),
            new WiredParamRule(LiteralAmount),
            new WiredEnumParamRule<WiredVariableTargetType>(
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.User,
                WiredVariableTargetType.Global,
                WiredVariableTargetType.Context
            ),
        ];

    public override int GetMaxVariableIds() => 2;

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
                AllVariablesHash = _ctx.Furni.AllVariablesHash,
            },
        ];

    public override async Task<bool> MutatePolicyAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        if (_wiredData.IntParams.Count < 4 || _wiredData.VariableIds.Count == 0)
        {
            return true;
        }

        HashSet<int> pool = FiltersFurni
            ? ctx.SelectorPool.SelectedFurniIds
            : ctx.SelectorPool.SelectedPlayerIds;

        int? amount = await ResolveAmountAsync(ctx, ct);

        if (amount is not int keep || keep < MinAmount || pool.Count <= keep)
        {
            return true;
        }

        if (
            !WiredVariableAccess.TryResolve(
                _ctx.Furni,
                _wiredData.VariableIds[0],
                out WiredVariableId id,
                out IWiredVariable? variable
            )
        )
        {
            return true;
        }

        WiredVariableSort sort = _wiredData.GetIntParam<WiredVariableSort>(1);

        // Only what the variable actually holds a rank for takes part: a furni with no value is not
        // "the lowest", it is simply not in the running.
        List<(int Target, long Rank)> ranked =
        [
            .. pool.Select(target => (Target: target, Rank: RankOf(variable!, id, target, sort)))
                .Where(entry => entry.Rank.HasValue)
                .Select(entry => (entry.Target, Rank: entry.Rank!.Value)),
        ];

        if (ranked.Count == 0)
        {
            return true;
        }

        IEnumerable<(int Target, long Rank)> ordered = sort.WantsDescending()
            ? ranked.OrderByDescending(entry => entry.Rank)
            : ranked.OrderBy(entry => entry.Rank);

        int[] kept = [.. ordered.Take(keep).Select(entry => entry.Target)];

        pool.Clear();
        pool.UnionWith(kept);

        return true;
    }

    private long? RankOf(
        IWiredVariable variable,
        WiredVariableId id,
        int target,
        WiredVariableSort sort
    )
    {
        WiredVariableKey key = new(id, RankedTarget, target);

        if (sort.RanksByValue())
        {
            return variable.TryGetValue(key, out WiredVariableValue value) ? value.Value : null;
        }

        if (!variable.TryGetTimestamps(key, out long createdAtMs, out long updatedAtMs))
        {
            return null;
        }

        long moment = sort.RanksByCreation() ? createdAtMs : updatedAtMs;

        // A moment the store never recorded is unknown, not the epoch — it would rank as the oldest
        // thing in the room.
        return moment > 0 ? moment : null;
    }

    /// <summary>The amount to keep: the number on the form, or a variable's value. Null when a
    /// variable was chosen and holds nothing — filtering to an unknown amount would empty the
    /// pool.</summary>
    private async Task<int?> ResolveAmountAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (_wiredData.GetIntParam<int>(2) == LiteralAmount)
        {
            return _wiredData.GetIntParam<int>(0);
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        return WiredVariableAccess.TryRead(
            _ctx.Furni,
            _wiredData.VariableIds.Count > 1 ? _wiredData.VariableIds[1] : string.Empty,
            _wiredData.GetIntParam<WiredVariableTargetType>(3),
            selection,
            out WiredVariableValue value
        )
            ? value.Value
            : null;
    }
}
