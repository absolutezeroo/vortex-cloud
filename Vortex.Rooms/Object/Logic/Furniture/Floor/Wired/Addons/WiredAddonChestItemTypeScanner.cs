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

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// "Scanner Add-on: Chest Furni of Type" — counts what the named chests hold and puts the number in
/// a variable, before the stack's conditions read it.
/// </summary>
/// <remarks>
/// <para>
/// The client says what it is for: <c>"This add-on scans Wired chest(s) for certain furni types, and
/// stores the amount of available items inside a context variable of choice"</c>. Slot 0 is the item
/// types — named by example, a furni of the kind meant — and slot 1 the chests, the same two slots
/// and the same counting rule as <see cref="Conditions.WiredConditionChestHasItemTypes"/>, which is
/// why both go through <c>CountChestItemsAsync</c> rather than growing a second way to count a
/// chest.
/// </para>
/// <para>
/// It runs in the policy phase, which is what makes it useful: the variable is written before the
/// conditions are evaluated, so a stack can scan a chest and then branch on how full it is in the
/// same firing.
/// </para>
/// <para>
/// The form's second scanning mode, <c>"Scan only previewed items"</c>, is not answered. A preview
/// here is a display choice — <c>WiredChestStore.BuildChestPreview</c> picks the kinds the chest
/// sprite shows, for two of its settings at random, and recomputes them every time somebody looks
/// inside — so "how many previewed items are there" has no answer that would still be true a second
/// later. Writing the full count instead would answer a question the builder did not ask, so the
/// box writes nothing and says so in the log.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_xtra_scan_chest_furni_by_type")]
public class WiredAddonChestItemTypeScanner(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>The form's first radio: count everything the chests hold.</summary>
    private const int ScanAllItems = 0;

    private static readonly WiredFurniSourceType[] FurniSources =
    [
        WiredFurniSourceType.SelectedItems,
        WiredFurniSourceType.SelectorItems,
        WiredFurniSourceType.SignalItems,
        WiredFurniSourceType.TriggeredItem,
    ];

    public override int WiredCode => (int)WiredAddonType.CHEST_ITEM_TYPE_SCANNER;

    // [0] = the scanning mode radio (ChestItemTypeScanner.readIntParamsFromForm).
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(0, 1, ScanAllItems)];

    /// <summary>One: the variable the count lands in.</summary>
    public override int GetMaxVariableIds() => 1;

    /// <summary>Slot 0 is the item types, slot 1 the chests — the form titles them in that order.</summary>
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [.. FurniSources],
            [.. FurniSources],
        ];

    /// <summary>The form picks from the room's variables, so the box has to carry that list.</summary>
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
        // An add-on that cannot do its job must not take the stack down with it: every early return
        // here is "nothing scanned", never "nothing fires".
        if (_wiredData.IntParams.Count == 0 || _wiredData.VariableIds.Count == 0)
        {
            return true;
        }

        if (_wiredData.GetIntParam<int>(0) != ScanAllItems)
        {
            _logger.LogDebug(
                "Wired chest scanner on item {ItemId} did not scan: the previewed-items mode has no stable answer here.",
                _ctx.ObjectId
            );

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

        int held = await _ctx.Chests.CountChestItemsAsync(GetStuffIds2(), GetStuffIds(), ct);

        // Give rather than Set, and not for lack of trying: Set takes an execution context and the
        // policy phase has none. Nothing is lost — in the store the two differ only in that Set
        // refuses a key that does not exist yet, both stamp and both mark dirty — and the one
        // variable that intercepts writes, the echo box, overrides Give as well, so a scan still
        // lands on whatever it is bound to.
        await variable!.GiveValueAsync(
            new WiredVariableKey(id, WiredVariableTargetType.Context, 0),
            held,
            replace: true
        );

        return true;
    }
}
