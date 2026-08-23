using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Offers a contract to the users the stack resolved.
/// </summary>
/// <remarks>
/// Int params from the client's form: [0] transaction mode, 0 normal, 1 multiplier, 2
/// auto-multiplier; [1] the multiplier, from a selector that can also read it off a wired variable;
/// [2] and [3] that selector's own two fields; [4] "automatically cancel trade after timeout";
/// [5] the timeout in seconds.
/// <para>
/// The multiplier is honoured only when it is a plain number. Sourced from a variable the offer goes
/// out at one, rather than at whatever literal happens to sit beside it: a contract is a price, and
/// the wrong price is worse than the plain one.
/// </para>
/// <para>
/// The contract is whichever of the box's own configured furni is one. One offer stands per player,
/// so offering again withdraws what was there, and a withdrawn offer counts as failed.
/// </para>
/// <para>
/// The terms are not the contract furni's — that one ships as plain furniture and carries no form.
/// They belong to the custom-contract add-on in this same stack, and without one there is nothing
/// to offer: a trade screen with no price on it is worse than none at all.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_act_init_transaction")]
public class WiredActionInitiateTransaction(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>The selector's "this is a plain number" option.</summary>
    private const int LiteralMultiplier = 0;

    /// <summary>The client's own checkbox: without it the seconds are not a timeout.</summary>
    private const int TimeoutEnabled = 1;

    /// <summary>Contracts carry their own logic — payment, reward and trade — so the box can tell
    /// one from any other furni it was pointed at. Matched on the logic and not the classname: a
    /// classname is not a key here.</summary>
    private const string ContractLogicPrefix = "wf_contract_";

    public override int WiredCode => (int)WiredActionType.INITIATE_TRANSACTION;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 2, 0), // transaction mode
            new WiredRangeParamRule(1, int.MaxValue, 1), // multiplier
            new WiredRangeParamRule(0, int.MaxValue, 0), // multiplier source
            new WiredRangeParamRule(0, int.MaxValue, 0), // multiplier source target
            new WiredRangeParamRule(0, 1, 0), // cancel on timeout
            new WiredRangeParamRule(0, int.MaxValue, 0), // timeout, seconds
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
        int multiplier = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 1;
        int source =
            _wiredData.IntParams.Count > 2 ? _wiredData.GetIntParam<int>(2) : LiteralMultiplier;
        int cancelOnTimeout = _wiredData.IntParams.Count > 4 ? _wiredData.GetIntParam<int>(4) : 0;
        int timeout = _wiredData.IntParams.Count > 5 ? _wiredData.GetIntParam<int>(5) : 0;

        if (source != LiteralMultiplier)
        {
            multiplier = 1;
        }

        if (!TryFindContract(out int contractId))
        {
            return true;
        }

        // The stock behind the counter. A box with none can still charge — it just has nothing to
        // hand back, which the contract's own terms are free to say.
        int chestId = GetStuffIds().FirstOrDefault();

        WiredAddonCustomContract? terms = ctx
            .Addons.OfType<WiredAddonCustomContract>()
            .FirstOrDefault();

        if (terms is null || !terms.TryBuildContract(mode, multiplier, out TradeContract? contract))
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            await _ctx
                .Transactions.OfferTransactionAsync(
                    contractId,
                    playerId,
                    chestId,
                    contract!,
                    mode,
                    multiplier,
                    cancelOnTimeout == TimeoutEnabled ? timeout : 0,
                    ct
                )
                .ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>The first of the box's configured furni that is a contract.</summary>
    private bool TryFindContract(out int contractId)
    {
        foreach (int furniId in GetStuffIds())
        {
            if (
                _ctx.Lookup.TryFindItem(furniId, out IRoomItem? item)
                && item.Definition.LogicName.StartsWith(
                    ContractLogicPrefix,
                    StringComparison.Ordinal
                )
            )
            {
                contractId = furniId;

                return true;
            }
        }

        contractId = 0;

        return false;
    }
}
