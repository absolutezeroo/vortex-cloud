using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Events.Player;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Contract transactions: a wired box offering someone a deal, and what becomes of it.
/// </summary>
/// <remarks>
/// A transaction is what the client calls a trade with a contract rather than with a person. A box
/// offers one (<c>wf_act_init_transaction</c>), the player accepts or refuses it on the trading
/// screen, and it ends as completed or failed — the two states the room raises triggers for.
/// <para>
/// Only the offer and the failure paths exist here. Completion is what the client's own trading
/// messages drive, and those are not mapped yet; the trigger is raised by
/// <see cref="CompleteTransactionAsync" /> so the day they are, the wiring is already listening.
/// </para>
/// <para>
/// The terms come from the custom-contract add-on in the offering box's own stack, which is where
/// they were typed. A player shown no terms is being asked to agree to a price nobody stated, so an
/// offer whose terms cannot be built is not made at all — the add-on says so by refusing to build
/// them, and the box does not call this.
/// </para>
/// </remarks>
public sealed partial class RoomGrain
{
    /// <summary>What is waiting on a player, at most one at a time — the client shows a single
    /// trading screen, so a second offer withdraws the first.</summary>
    private readonly Dictionary<PlayerId, PendingTransaction> _pendingTransactions = [];

    private sealed record PendingTransaction(
        int ContractId,
        int Mode,
        int Multiplier,
        DateTime? ExpiresAt
    );

    /// <summary>
    /// Drops anything that has run out of time, and says who was dropped.
    /// </summary>
    /// <remarks>
    /// Checked whenever a transaction is touched rather than on the room clock. A timeout only
    /// matters at the moment someone asks about the transaction, and a tick that exists to notice
    /// nothing most of the time is a tick not worth paying for.
    /// </remarks>
    private List<PlayerId> ExpireTimedOutTransactions()
    {
        DateTime now = DateTime.UtcNow;

        List<PlayerId> expired =
        [
            .. _pendingTransactions
                .Where(entry => entry.Value.ExpiresAt is { } deadline && deadline <= now)
                .Select(entry => entry.Key),
        ];

        foreach (PlayerId playerId in expired)
        {
            _pendingTransactions.Remove(playerId);
        }

        return expired;
    }

    private Task RaiseTransactionFailedAsync(PlayerId playerId, CancellationToken ct) =>
        PublishRoomEventAsync(
            new WiredTransactionFailedEvent
            {
                RoomId = _state.RoomId,
                CausedBy = ActionContext.CreateForPlayer(playerId, RoomId),
                PlayerId = playerId,
            },
            ct
        );

    public async Task<bool> OfferTransactionAsync(
        int contractId,
        PlayerId playerId,
        TradeContract contract,
        int mode,
        int multiplier,
        int timeoutSeconds,
        CancellationToken ct
    )
    {
        if (contractId <= 0 || playerId <= 0 || !_state.ItemsById.ContainsKey(contractId))
        {
            return false;
        }

        foreach (PlayerId timedOut in ExpireTimedOutTransactions())
        {
            await RaiseTransactionFailedAsync(timedOut, ct).ConfigureAwait(true);
        }

        // Replacing an offer is withdrawing it, and a withdrawn offer failed.
        if (_pendingTransactions.Remove(playerId))
        {
            await RaiseTransactionFailedAsync(playerId, ct).ConfigureAwait(true);
        }

        _pendingTransactions[playerId] = new PendingTransaction(
            contractId,
            mode,
            Math.Max(1, multiplier),
            timeoutSeconds > 0 ? DateTime.UtcNow.AddSeconds(timeoutSeconds) : null
        );

        await _grainFactory
            .GetPlayerPresenceGrain(playerId)
            .SendComposerAsync(
                new WiredTradeInitiateMessageComposer
                {
                    RequirementType = CustomRequirement,
                    YouGetText = string.Empty,
                    LayoutType = string.Empty,
                    // A price is the whole point of a contract, so it is on screen before the
                    // player puts anything up rather than after.
                    ShowRequirementsImmediate = true,
                    OverridePreviousTrade = true,
                    TimeoutSeconds = timeoutSeconds,
                    Contract = contract,
                }
            )
            .ConfigureAwait(true);

        _logger.LogDebug(
            "Contract {ContractId} offered to player {PlayerId} in room {RoomId}.",
            contractId,
            playerId,
            RoomId
        );

        return true;
    }

    /// <summary>The requirement type whose payload carries terms; the other three carry none.</summary>
    private const int CustomRequirement = 4;

    public async Task<int> CancelTransactionAsync(
        int contractId,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        foreach (PlayerId timedOut in ExpireTimedOutTransactions())
        {
            await RaiseTransactionFailedAsync(timedOut, ct).ConfigureAwait(true);
        }

        if (
            playerId <= 0
            || !_pendingTransactions.TryGetValue(playerId, out PendingTransaction? pending)
        )
        {
            return 0;
        }

        // contractId 0 is the client's "any ongoing transaction"; anything else has to match.
        if (contractId > 0 && pending.ContractId != contractId)
        {
            return 0;
        }

        _pendingTransactions.Remove(playerId);

        await RaiseTransactionFailedAsync(playerId, ct).ConfigureAwait(true);

        return 1;
    }

    /// <summary>
    /// Closes a transaction as done and raises the trigger that waits on it.
    /// </summary>
    /// <remarks>
    /// Nothing calls this yet: what completes a transaction is the client accepting on its trading
    /// screen, and those messages are not mapped. It exists so the completion trigger has one place
    /// to be raised from rather than several once they are.
    /// </remarks>
    public async Task<bool> CompleteTransactionAsync(PlayerId playerId, CancellationToken ct)
    {
        if (!_pendingTransactions.Remove(playerId))
        {
            return false;
        }

        await PublishRoomEventAsync(
                new WiredTransactionCompletedEvent
                {
                    RoomId = _state.RoomId,
                    CausedBy = ActionContext.CreateForPlayer(playerId, RoomId),
                    PlayerId = playerId,
                },
                ct
            )
            .ConfigureAwait(true);

        return true;
    }
}
