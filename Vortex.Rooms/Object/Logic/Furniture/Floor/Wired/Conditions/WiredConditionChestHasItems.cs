using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The chest holds this many items" — the question the two payout actions never let a stack ask.
/// </summary>
/// <remarks>
/// A room could already hand furniture and credits out of a wired chest, and had no way to find out
/// whether one was empty first. The prize machine kept firing, the trigger kept flashing, and
/// nothing came out.
/// <para>
/// Int params from the client's setup form (conditions/chests/ChestHasAmount): [0] the amount,
/// [1] where that amount comes from (0 = the number typed in the box, anything else = a wired
/// variable), [2] that source's target, [3] the comparison, whose radio values are the client's own
/// and not in reading order — 0 <c>&lt;</c>, 1 <c>=</c>, 2 <c>&gt;</c>, 3 <c>≤</c>, 4 <c>≠</c>,
/// 5 <c>≥</c>.
/// </para>
/// <para>
/// Only a literal amount is honoured. An amount sourced from a variable makes the condition fail
/// rather than compare against the number that happens to sit beside it, which is the same call
/// <see cref="Actions.WiredActionGiveFurniFromChest"/> makes on its own count — a wrong threshold
/// here empties a chest the builder meant to protect.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_cnd_chest_has_items")]
public class WiredConditionChestHasItems(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>The form's "the amount is the number I typed" option.</summary>
    protected const int LiteralAmount = 0;

    private int _held;

    private bool _counted;

    public override int WiredCode => (int)WiredConditionType.CHEST_HAS_ITEMS;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 1_000_000, 0), // amount, the form's own slider bounds
            new WiredRangeParamRule(0, int.MaxValue, 0), // where the amount comes from
            new WiredRangeParamRule(0, int.MaxValue, 0), // that source's target
            new WiredRangeParamRule(0, 5, 0), // comparison
        ];

    /// <summary>
    /// Two furni slots, and the second is not a second pick.
    /// </summary>
    /// <remarks>
    /// Slot 0 is the chests. Slot 1 exists because the form's amount is a merged input source, and
    /// the client resolves it through <c>mergedSelections()[0]</c>, which this box returns as
    /// <c>[1, 0]</c> — furni slot 1, user slot 0. Leaving either undeclared makes the picker read
    /// past the end of the list it was handed and takes the whole wired dialog down with it, the
    /// same way it did for the give-furni action.
    /// </remarks>
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [.. FurniSources],
            [.. FurniSources],
        ];

    /// <inheritdoc cref="GetAllowedFurniSources"/>
    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [.. PlayerSources],
        ];

    protected static readonly WiredFurniSourceType[] FurniSources =
    [
        WiredFurniSourceType.SelectedItems,
        WiredFurniSourceType.SelectorItems,
        WiredFurniSourceType.SignalItems,
        WiredFurniSourceType.TriggeredItem,
    ];

    protected static readonly WiredPlayerSourceType[] PlayerSources =
    [
        WiredPlayerSourceType.TriggeredUser,
        WiredPlayerSourceType.SelectorUsers,
        WiredPlayerSourceType.SignalUsers,
    ];

    /// <summary>The chests to count, which for this box is its own first slot.</summary>
    protected virtual List<int> GetChestIds() => GetStuffIds();

    /// <summary>The furni whose kinds narrow the count. This box counts everything.</summary>
    protected virtual List<int> GetKindExampleIds() => [];

    public override async Task PrepareAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        _counted = false;

        if (_wiredData.IntParams.Count < 4 || _wiredData.GetIntParam<int>(1) != LiteralAmount)
        {
            return;
        }

        _held = await _ctx.Chests.CountChestItemsAsync(GetChestIds(), GetKindExampleIds(), ct);
        _counted = true;
    }

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        // No count was taken: the box is configured to read its amount from a variable, or the
        // prepare step never ran. Either way there is nothing to compare.
        if (!_counted)
        {
            return IsNegative();
        }

        int amount = _wiredData.GetIntParam<int>(0);

        bool result = _wiredData.GetIntParam<int>(3) switch
        {
            0 => _held < amount,
            1 => _held == amount,
            2 => _held > amount,
            3 => _held <= amount,
            4 => _held != amount,
            5 => _held >= amount,
            _ => false,
        };

        return IsNegative() ? !result : result;
    }
}
