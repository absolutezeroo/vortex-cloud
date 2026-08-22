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
/// Calls off a contract waiting on the users the stack resolved.
/// </summary>
/// <remarks>
/// One int param, the client's own two choices: 0 cancels the contract the box points at, 1 cancels
/// any transaction the player has open. Each cancellation raises the failure trigger, which is how a
/// stack notices an offer being withdrawn.
/// </remarks>
[RoomObjectLogic("wf_act_cancel_transaction")]
public class WiredActionCancelTransaction(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>"Any ongoing transaction", as opposed to the contract this box names.</summary>
    private const int AnyTransaction = 1;

    /// <summary>Contracts carry their own logic — payment, reward and trade — so the box can tell
    /// one from any other furni it was pointed at. Matched on the logic and not the classname: a
    /// classname is not a key here.</summary>
    private const string ContractLogicPrefix = "wf_contract_";

    public override int WiredCode => (int)WiredActionType.CANCEL_TRANSACTION;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 1, 0), // match criteria
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
        int criteria = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int contractId = criteria == AnyTransaction ? 0 : FindContract();

        // "Specified contract" with no contract configured cancels nothing, rather than everything.
        if (criteria != AnyTransaction && contractId == 0)
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            await _ctx
                .Transactions.CancelTransactionAsync(contractId, playerId, ct)
                .ConfigureAwait(false);
        }

        return true;
    }

    private int FindContract()
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
                return furniId;
            }
        }

        return 0;
    }
}
