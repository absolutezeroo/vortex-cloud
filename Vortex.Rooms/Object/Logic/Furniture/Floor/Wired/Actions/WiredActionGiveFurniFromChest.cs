using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Hands furniture out of a wired chest to the users the stack resolved.
/// </summary>
/// <remarks>
/// Int params from the client's setup form (actiontypes/chests): [0] rewarding mode, 0 "give
/// specified amount" and 1 "give all"; [1] how many items; [2] and [3] the selector that lets the
/// count come from a wired variable instead of the box; [4] the form's "show by default" checkbox;
/// [5] the iteration mode this form adds on top of the currency one, which is read so the params
/// line up and not acted on -- what it iterates over is not established from the client.
/// <para>
/// Only the literal count is honoured. When the form says it comes from a variable this action hands
/// out nothing rather than the literal that happens to sit beside it: these are real items leaving a
/// chest someone filled, and the wrong number of them is worse than none.
/// </para>
/// <para>
/// The chest is whichever of the box's own configured furni is one, and it has to be a furniture
/// chest rather than a currency one. Nothing leaves a locked chest, and the ledger records it as a
/// WIRED movement rather than a manual one.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_act_give_furni")]
public class WiredActionGiveFurniFromChest(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>"Give all", as opposed to the specified amount.</summary>
    private const int GiveEverything = 1;

    /// <summary>The selector's "the count is a plain number" option. Anything else means it is
    /// sourced from a wired variable, which is not read yet.</summary>
    private const int LiteralAmount = 0;

    public override int WiredCode => (int)WiredActionType.GIVE_FURNI_FROM_CHEST;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 1, 0), // rewarding mode
            new WiredRangeParamRule(1, int.MaxValue, 1), // how many items
            new WiredRangeParamRule(0, int.MaxValue, 0), // count source
            new WiredRangeParamRule(0, int.MaxValue, 0), // count source target
            new WiredRangeParamRule(0, 1, 0), // show by default
            new WiredRangeParamRule(0, int.MaxValue, 0), // iteration mode
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        int mode = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int count = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        int source =
            _wiredData.IntParams.Count > 2 ? _wiredData.GetIntParam<int>(2) : LiteralAmount;

        // "Give all" of a furniture chest still needs a number to stop at, and the chest itself is
        // the only sane one: it hands over what it holds.
        if (count <= 0 || source != LiteralAmount)
        {
            if (mode != GiveEverything)
            {
                return true;
            }

            count = int.MaxValue;
        }

        if (!TryFindChest(out int chestId))
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            await _ctx
                .Chests.PayOutChestItemsAsync(chestId, playerId, count, ct)
                .ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>The first of the box's configured furni that is actually a chest.</summary>
    private bool TryFindChest(out int chestId)
    {
        foreach (int furniId in GetStuffIds())
        {
            if (
                _ctx.Lookup.TryFindItem(furniId, out IRoomItem? item)
                && item.Definition.Name.StartsWith("wf_storage_", StringComparison.Ordinal)
            )
            {
                chestId = furniId;

                return true;
            }
        }

        chestId = 0;

        return false;
    }
}
