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
/// Pays credits out of a wired chest to the users the stack resolved.
/// </summary>
/// <remarks>
/// Int params from the client's setup form (actiontypes/chests): [0] rewarding mode, 0 "give
/// specified amount" and 1 "give all"; [1] the amount, which the form bounds at 1; [2] and [3] the
/// selector that lets the amount come from a wired variable instead of the box; [4] the form's
/// "show by default" checkbox.
/// <para>
/// Only the literal amount is honoured. When the form says the amount comes from a variable this
/// action pays nothing rather than paying the literal that happens to sit beside it — a wired box
/// that hands out the wrong number of credits is worse than one that hands out none, and this is
/// real currency leaving a chest someone filled.
/// </para>
/// <para>
/// The chest is whichever of the box's own configured furni is one. Nothing is paid out of a locked
/// chest, and the ledger records it as a WIRED movement rather than a manual one.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_act_give_currency")]
public class WiredActionGiveCurrencyFromChest(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>"Give all", as opposed to the specified amount.</summary>
    private const int GiveEverything = 1;

    /// <summary>The selector's "the amount is a plain number" option. Anything else means it is
    /// sourced from a wired variable, which is not read yet.</summary>
    private const int LiteralAmount = 0;

    /// <summary>The logic a currency chest carries.</summary>
    private const string ChestLogic = "furniture_coinschest";

    public override int WiredCode => (int)WiredActionType.GIVE_CURRENCY_FROM_CHEST;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 1, 0), // rewarding mode
            new WiredRangeParamRule(1, int.MaxValue, 1), // amount
            new WiredRangeParamRule(0, int.MaxValue, 0), // amount source
            new WiredRangeParamRule(0, int.MaxValue, 0), // amount source target
            new WiredRangeParamRule(0, 1, 0), // show by default
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
        int amount = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        int source =
            _wiredData.IntParams.Count > 2 ? _wiredData.GetIntParam<int>(2) : LiteralAmount;
        bool everything = mode == GiveEverything;

        if (!everything && (amount <= 0 || source != LiteralAmount))
        {
            return true;
        }

        if (!TryFindChest(out int chestId))
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            await _ctx
                .Chests.PayOutChestCreditsAsync(chestId, playerId, amount, everything, ct)
                .ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// The first of the box's configured furni that is a currency chest.
    /// </summary>
    /// <remarks>
    /// Matched on the logic, not the classname: a classname is not a key in this database and the
    /// logic is the one value that says what the furni actually is.
    /// </remarks>
    private bool TryFindChest(out int chestId)
    {
        foreach (int furniId in GetStuffIds())
        {
            if (
                _ctx.Lookup.TryFindItem(furniId, out IRoomItem? item)
                && string.Equals(item.Definition.LogicName, ChestLogic, StringComparison.Ordinal)
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
