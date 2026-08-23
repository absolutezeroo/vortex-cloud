using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Wired;

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
/// The terms are the contract furni's own — read off its add-on form, turned into the shape the
/// trading screen reads, and sent with the offer. A player shown no terms is being asked to agree
/// to a price nobody stated, so an offer whose terms cannot be built is not made at all.
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

        if (!TryBuildContract(contractId, mode, Math.Max(1, multiplier), out TradeContract? terms))
        {
            return false;
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
                    Contract = terms,
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

    /// <summary>The add-on form's own two element types.</summary>
    private const int CoinTerm = 0;

    /// <summary>The amount selector's "this is a plain number" option.</summary>
    private const int LiteralAmount = 0;

    /// <summary>
    /// Turns a contract furni's add-on form into the terms the trading screen reads.
    /// </summary>
    /// <remarks>
    /// Ten ints, five per side: enabled, coin/furni, where the amount comes from, the amount, and
    /// the variable's target — with each side's accepted furni in its own picker, <c>StuffIds</c>
    /// for payment and <c>StuffIds2</c> for reward.
    /// <para>
    /// The two sides are not symmetric on the wire and so not here either: payment is a list of
    /// rules and reward a single one, and rules are alternatives where the nodes inside one are a
    /// bundle. So several furni to pay with are choices — any one of them settles the price — and
    /// several furni to receive are all of them.
    /// </para>
    /// <para>
    /// An amount held in a wired variable is refused rather than guessed at: resolving one needs
    /// the execution context the offer no longer has, and a term at the wrong amount is worse than
    /// no offer — the same reading <c>wf_act_init_transaction</c> already takes for its multiplier.
    /// </para>
    /// </remarks>
    private bool TryBuildContract(
        int contractId,
        int mode,
        int multiplier,
        out TradeContract? contract
    )
    {
        contract = null;

        if (
            !_state.ItemsById.TryGetValue(contractId, out IRoomItem? item)
            || item.Logic is not IWiredBox box
        )
        {
            return false;
        }

        WiredDataSnapshot data = box.GetSnapshot();

        if (data.IntParams.Count < 10)
        {
            return false;
        }

        if (
            !TryBuildSide(data, offset: 0, data.StuffIds, out ImmutableArray<TradeContractRule> pay)
            || !TryBuildSide(
                data,
                offset: 5,
                data.StuffIds2,
                out ImmutableArray<TradeContractRule> get
            )
        )
        {
            _logger.LogWarning(
                "Contract {ContractId} in room {RoomId} asks for an amount held in a wired "
                    + "variable, which cannot be read here — no offer was made.",
                contractId,
                RoomId
            );

            return false;
        }

        if (pay.IsEmpty && get.IsEmpty)
        {
            return false;
        }

        contract = new TradeContract
        {
            YouGiveRules = pay.IsEmpty ? null : pay,
            // The receive side is one rule: everything it names comes back together.
            YouGetRule = get.IsEmpty
                ? null
                : new TradeContractRule { Nodes = [.. get.SelectMany(rule => rule.Nodes)] },
            Mode = mode,
            Multiplier = multiplier,
            AutoMultiplierMax = multiplier,
        };

        return true;
    }

    /// <summary>
    /// One side's terms, or false when its amount is not a plain number.
    /// </summary>
    /// <remarks>
    /// A side that is switched off contributes nothing and is not a failure — a payment-only
    /// contract is exactly that, and so is a reward-only one.
    /// </remarks>
    private bool TryBuildSide(
        WiredDataSnapshot data,
        int offset,
        List<int> furniIds,
        out ImmutableArray<TradeContractRule> rules
    )
    {
        rules = ImmutableArray<TradeContractRule>.Empty;

        if (data.IntParams[offset] == 0)
        {
            return true;
        }

        if (data.IntParams[offset + 2] != LiteralAmount)
        {
            return false;
        }

        int amount = Math.Max(1, data.IntParams[offset + 3]);

        if (data.IntParams[offset + 1] == CoinTerm)
        {
            rules =
            [
                new TradeContractRule
                {
                    Nodes = [new TradeContractNode { IsFurni = false, Amount = amount }],
                },
            ];

            return true;
        }

        rules =
        [
            .. furniIds
                .Select(furniId =>
                    _state.ItemsById.TryGetValue(furniId, out IRoomItem? furni) ? furni : null
                )
                .Where(furni => furni is not null)
                // The same kind twice is one choice, not two: the picker holds instances, and a
                // term names a kind.
                .DistinctBy(furni => furni!.Definition.SpriteId)
                .Select(furni => new TradeContractRule
                {
                    Nodes =
                    [
                        new TradeContractNode
                        {
                            IsFurni = true,
                            Amount = amount,
                            ItemType = new TradeContractItemType
                            {
                                IsWallItem = furni!.Definition.ProductType == ProductType.Wall,
                                SpriteId = furni.Definition.SpriteId,
                                LegacyPosterId = string.Empty,
                            },
                        },
                    ],
                }),
        ];

        return true;
    }

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
