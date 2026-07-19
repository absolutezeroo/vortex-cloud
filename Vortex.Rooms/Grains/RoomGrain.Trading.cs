using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Trading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    // Client TradeOpenFailed reasons 7/8 pop a generic alert; any other value shows the templated
    // "could not trade with {name}" message, which fits every gating failure here.
    private const int TradeOpenFailedReason = 6;

    // TradingClose reason 1 is the client's commit-error alert; 0 is an ordinary cancellation.
    private const int TradeCloseCancelled = 0;
    private const int TradeCloseCommitError = 1;

    public async Task OpenTradeAsync(
        PlayerId requesterId,
        int otherRoomObjectId,
        CancellationToken ct
    )
    {
        if (!_state.AvatarsByPlayerId.ContainsKey(requesterId))
        {
            return;
        }

        if (
            !TryResolvePlayerByObjectId(otherRoomObjectId, out PlayerId otherId)
            || otherId == requesterId
        )
        {
            await SendTradeOpenFailedAsync(requesterId, otherId, ct).ConfigureAwait(true);
            return;
        }

        if (
            _state.TradeSessionsByPlayerId.ContainsKey(requesterId)
            || _state.TradeSessionsByPlayerId.ContainsKey(otherId)
        )
        {
            await SendTradeOpenFailedAsync(requesterId, otherId, ct).ConfigureAwait(true);
            return;
        }

        bool requesterCanTrade = await CanTradeInRoomAsync(requesterId, ct).ConfigureAwait(true);
        bool otherCanTrade = await CanTradeInRoomAsync(otherId, ct).ConfigureAwait(true);

        if (!requesterCanTrade || !otherCanTrade)
        {
            await SendTradeOpenFailedAsync(requesterId, otherId, ct).ConfigureAwait(true);
            return;
        }

        RoomObjectId requesterObjectId = _state.AvatarsByPlayerId[requesterId];
        RoomObjectId otherObjectId = _state.AvatarsByPlayerId[otherId];

        RoomTradeSession session = new()
        {
            UserOneId = requesterId,
            UserTwoId = otherId,
            UserOneObjectId = requesterObjectId,
            UserTwoObjectId = otherObjectId,
        };

        _state.TradeSessionsByPlayerId[requesterId] = session;
        _state.TradeSessionsByPlayerId[otherId] = session;

        await SendToTradeAsync(
                session,
                new TradingOpenEventMessageComposer
                {
                    UserId = requesterObjectId,
                    UserCanTrade = true,
                    OtherUserId = otherObjectId,
                    OtherUserCanTrade = true,
                }
            )
            .ConfigureAwait(true);

        await _events
            .PublishAsync(
                new TradeStartedEvent(requesterId.Value, otherId.Value, _state.RoomId.Value),
                ct
            )
            .ConfigureAwait(true);
    }

    public async Task AddTradeItemsAsync(
        PlayerId requesterId,
        IReadOnlyList<int> itemIds,
        CancellationToken ct
    )
    {
        if (!TryGetSession(requesterId, out RoomTradeSession? session))
        {
            await SendTradingNotOpenAsync(requesterId, ct).ConfigureAwait(true);
            return;
        }

        if (session.Phase != TradePhase.Building)
        {
            return;
        }

        List<int> offer = session.ItemsOf(requesterId);
        IInventoryGrain inventory = _grainFactory.GetInventoryGrain(requesterId);
        bool changed = false;

        foreach (int itemId in itemIds)
        {
            if (offer.Contains(itemId) || offer.Count >= _roomConfig.MaxTradeItemsPerSide)
            {
                continue;
            }

            FurnitureItemSnapshot? snapshot = await inventory
                .GetItemSnapshotAsync(itemId, ct)
                .ConfigureAwait(true);

            // An item the requester no longer owns or that is flagged non-tradeable is silently
            // dropped from the offer -- there is no distinct "rejected item" composer in this client,
            // and it will simply be absent from the item list the other party sees.
            if (snapshot is null || !snapshot.Definition.CanTrade)
            {
                continue;
            }

            offer.Add(itemId);
            changed = true;
        }

        if (changed)
        {
            session.ResetAgreement();
            await BroadcastItemListAsync(session, ct).ConfigureAwait(true);
        }
    }

    public async Task RemoveTradeItemAsync(PlayerId requesterId, int itemId, CancellationToken ct)
    {
        if (!TryGetSession(requesterId, out RoomTradeSession? session))
        {
            return;
        }

        if (session.Phase != TradePhase.Building)
        {
            return;
        }

        if (!session.ItemsOf(requesterId).Remove(itemId))
        {
            return;
        }

        session.ResetAgreement();
        await BroadcastItemListAsync(session, ct).ConfigureAwait(true);
    }

    public async Task SetTradeAcceptAsync(PlayerId requesterId, bool accepted, CancellationToken ct)
    {
        if (!TryGetSession(requesterId, out RoomTradeSession? session))
        {
            return;
        }

        if (session.Phase != TradePhase.Building || session.AcceptedOf(requesterId) == accepted)
        {
            return;
        }

        session.SetAccepted(requesterId, accepted);

        await SendToTradeAsync(
                session,
                new TradingAcceptEventMessageComposer
                {
                    UserId = session.ObjectIdOf(requesterId),
                    UserAccepts = accepted,
                }
            )
            .ConfigureAwait(true);

        if (session.BothAccepted)
        {
            session.Phase = TradePhase.Confirming;

            await SendToTradeAsync(session, new TradingConfirmationEventMessageComposer())
                .ConfigureAwait(true);
        }
    }

    public async Task ConfirmTradeAsync(PlayerId requesterId, bool confirm, CancellationToken ct)
    {
        if (!TryGetSession(requesterId, out RoomTradeSession? session))
        {
            return;
        }

        if (session.Phase != TradePhase.Confirming)
        {
            return;
        }

        if (!confirm)
        {
            await EndTradeAsync(
                    session,
                    session.ObjectIdOf(requesterId),
                    TradeCloseCancelled,
                    "declined",
                    [session.UserOneId, session.UserTwoId],
                    ct
                )
                .ConfigureAwait(true);
            return;
        }

        session.SetConfirmed(requesterId, true);

        if (session.BothConfirmed)
        {
            await CommitTradeAsync(session, ct).ConfigureAwait(true);
        }
    }

    public async Task CloseTradeAsync(PlayerId requesterId, CancellationToken ct)
    {
        if (!TryGetSession(requesterId, out RoomTradeSession? session))
        {
            return;
        }

        await EndTradeAsync(
                session,
                session.ObjectIdOf(requesterId),
                TradeCloseCancelled,
                "cancelled",
                [session.UserOneId, session.UserTwoId],
                ct
            )
            .ConfigureAwait(true);
    }

    /// <summary>Tears down any trade a leaving/disconnecting player is in, notifying the participant
    /// who is still present. Called from <c>RoomGrain.Avatar.cs</c> when an avatar is removed.</summary>
    internal async Task CloseTradeForLeavingPlayerAsync(PlayerId playerId, CancellationToken ct)
    {
        if (!TryGetSession(playerId, out RoomTradeSession? session))
        {
            return;
        }

        PlayerId remaining = session.OtherOf(playerId);

        await EndTradeAsync(
                session,
                session.ObjectIdOf(playerId),
                TradeCloseCancelled,
                "left",
                [remaining],
                ct
            )
            .ConfigureAwait(true);
    }

    private async Task CommitTradeAsync(RoomTradeSession session, CancellationToken ct)
    {
        bool swapped = false;

        try
        {
            // Re-validate ownership and tradeability right before committing: an item may have been
            // placed, sold, or traded elsewhere while the confirm dialog was open.
            if (
                !await ValidateOfferAsync(session.UserOneId, session.UserOneItemIds, ct)
                    .ConfigureAwait(true)
                || !await ValidateOfferAsync(session.UserTwoId, session.UserTwoItemIds, ct)
                    .ConfigureAwait(true)
            )
            {
                await AbortCommitAsync(session, ct).ConfigureAwait(true);
                return;
            }

            swapped = await TryPersistOwnershipSwapAsync(session, ct).ConfigureAwait(true);

            if (!swapped)
            {
                await AbortCommitAsync(session, ct).ConfigureAwait(true);
                return;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(
                ex,
                "Trade commit failed between {UserOne} and {UserTwo} in room {RoomId}.",
                session.UserOneId,
                session.UserTwoId,
                _state.RoomId
            );

            if (!swapped)
            {
                await AbortCommitAsync(session, ct).ConfigureAwait(true);
                return;
            }
        }

        int[] oneItems = [.. session.UserOneItemIds];
        int[] twoItems = [.. session.UserTwoItemIds];

        RemoveSession(session);

        // Ownership is persisted; the rest is best-effort resync/notification. A stale inventory
        // cache self-heals on the next reload, so a failure here can never duplicate or lose an item.
        try
        {
            await _grainFactory
                .GetInventoryGrain(session.UserOneId)
                .ApplyTradeResultAsync(oneItems, twoItems, ct)
                .ConfigureAwait(true);
            await _grainFactory
                .GetInventoryGrain(session.UserTwoId)
                .ApplyTradeResultAsync(twoItems, oneItems, ct)
                .ConfigureAwait(true);

            // Per-item forensics (item_events) plus one trade-level business-audit record that the
            // dashboard's Item audit surface reads.
            await PublishTradeAuditAsync(session.UserOneId, session.UserTwoId, oneItems, ct)
                .ConfigureAwait(true);
            await PublishTradeAuditAsync(session.UserTwoId, session.UserOneId, twoItems, ct)
                .ConfigureAwait(true);
            await _events
                .PublishAsync(
                    new TradeCompletedEvent(
                        session.UserOneId.Value,
                        session.UserTwoId.Value,
                        oneItems,
                        twoItems,
                        _state.RoomId.Value
                    ),
                    ct
                )
                .ConfigureAwait(true);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Trade committed but post-commit resync failed in room {RoomId}.",
                _state.RoomId
            );
        }

        await Task.WhenAll(
                _grainFactory
                    .GetPlayerPresenceGrain(session.UserOneId)
                    .SendComposerAsync(new TradingCompletedEventMessageComposer()),
                _grainFactory
                    .GetPlayerPresenceGrain(session.UserTwoId)
                    .SendComposerAsync(new TradingCompletedEventMessageComposer())
            )
            .ConfigureAwait(true);
    }

    private async Task<bool> ValidateOfferAsync(
        PlayerId playerId,
        IReadOnlyList<int> itemIds,
        CancellationToken ct
    )
    {
        if (itemIds.Count == 0)
        {
            return true;
        }

        IInventoryGrain inventory = _grainFactory.GetInventoryGrain(playerId);

        foreach (int itemId in itemIds)
        {
            FurnitureItemSnapshot? snapshot = await inventory
                .GetItemSnapshotAsync(itemId, ct)
                .ConfigureAwait(true);

            if (snapshot is null || !snapshot.Definition.CanTrade)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Swaps furniture ownership for both offers inside a single database transaction, so the
    /// exchange is all-or-nothing: either every item changes hands or none does. Guards that each row
    /// is still owned by its offerer and not placed in a room before flipping <c>player_id</c>.</summary>
    private async Task<bool> TryPersistOwnershipSwapAsync(
        RoomTradeSession session,
        CancellationToken ct
    )
    {
        List<int> oneIds = session.UserOneItemIds;
        List<int> twoIds = session.UserTwoItemIds;
        int oneOwner = session.UserOneId.Value;
        int twoOwner = session.UserTwoId.Value;

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            await using IDbContextTransaction tx = await dbCtx
                .Database.BeginTransactionAsync(ct)
                .ConfigureAwait(true);

            List<FurnitureEntity> oneRows = await dbCtx
                .Furnitures.Where(f => oneIds.Contains(f.Id))
                .ToListAsync(ct)
                .ConfigureAwait(true);
            List<FurnitureEntity> twoRows = await dbCtx
                .Furnitures.Where(f => twoIds.Contains(f.Id))
                .ToListAsync(ct)
                .ConfigureAwait(true);

            if (
                !IsOfferPersistable(oneRows, oneIds.Count, oneOwner)
                || !IsOfferPersistable(twoRows, twoIds.Count, twoOwner)
            )
            {
                return false;
            }

            foreach (FurnitureEntity row in oneRows)
            {
                row.PlayerEntityId = twoOwner;
            }

            foreach (FurnitureEntity row in twoRows)
            {
                row.PlayerEntityId = oneOwner;
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            await tx.CommitAsync(ct).ConfigureAwait(true);

            return true;
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
        }
    }

    private static bool IsOfferPersistable(
        List<FurnitureEntity> rows,
        int expectedCount,
        int expectedOwner
    ) =>
        rows.Count == expectedCount
        && rows.All(r => r.PlayerEntityId == expectedOwner && r.RoomEntityId is null);

    private async Task PublishTradeAuditAsync(
        PlayerId fromOwner,
        PlayerId toOwner,
        int[] itemIds,
        CancellationToken ct
    )
    {
        foreach (int itemId in itemIds)
        {
            await _events
                .PublishAsync(
                    new ItemTradedEvent(
                        itemId,
                        fromOwner.Value,
                        fromOwner.Value,
                        toOwner.Value,
                        _state.RoomId.Value
                    ),
                    ct
                )
                .ConfigureAwait(true);
        }
    }

    private async Task AbortCommitAsync(RoomTradeSession session, CancellationToken ct)
    {
        await EndTradeAsync(
                session,
                session.UserOneObjectId,
                TradeCloseCommitError,
                "commit_error",
                [session.UserOneId, session.UserTwoId],
                ct
            )
            .ConfigureAwait(true);
    }

    private async Task EndTradeAsync(
        RoomTradeSession session,
        RoomObjectId cancellerObjectId,
        int reason,
        string auditReason,
        IReadOnlyList<PlayerId> notify,
        CancellationToken ct
    )
    {
        PlayerId userOne = session.UserOneId;
        PlayerId userTwo = session.UserTwoId;

        RemoveSession(session);

        TradingCloseEventMessageComposer composer = new()
        {
            UserId = cancellerObjectId,
            Reason = reason,
        };

        await Task.WhenAll(
                notify.Select(id =>
                    _grainFactory.GetPlayerPresenceGrain(id).SendComposerAsync(composer)
                )
            )
            .ConfigureAwait(true);

        await _events
            .PublishAsync(
                new TradeCancelledEvent(
                    userOne.Value,
                    userTwo.Value,
                    _state.RoomId.Value,
                    auditReason
                ),
                ct
            )
            .ConfigureAwait(true);
    }

    private void RemoveSession(RoomTradeSession session)
    {
        _state.TradeSessionsByPlayerId.Remove(session.UserOneId);
        _state.TradeSessionsByPlayerId.Remove(session.UserTwoId);
    }

    private bool TryGetSession(PlayerId playerId, out RoomTradeSession session)
    {
        if (
            _state.TradeSessionsByPlayerId.TryGetValue(playerId, out RoomTradeSession? found)
            && found.IsParticipant(playerId)
        )
        {
            session = found;
            return true;
        }

        session = null!;
        return false;
    }

    private bool TryResolvePlayerByObjectId(int objectId, out PlayerId playerId)
    {
        foreach ((PlayerId candidate, RoomObjectId candidateObjectId) in _state.AvatarsByPlayerId)
        {
            if (candidateObjectId.Value == objectId)
            {
                playerId = candidate;
                return true;
            }
        }

        playerId = default;
        return false;
    }

    private async Task<bool> CanTradeInRoomAsync(PlayerId playerId, CancellationToken ct)
    {
        RoomTradeModeType mode = _state.RoomSnapshot.TradeType;

        if (mode == RoomTradeModeType.Disabled)
        {
            return false;
        }

        if (mode == RoomTradeModeType.Everyone)
        {
            return true;
        }

        RoomControllerType level = await GetControllerLevelAsync(
                ActionContext.CreateForPlayer(playerId, _state.RoomId),
                ct
            )
            .ConfigureAwait(true);

        return level >= RoomControllerType.Rights;
    }

    private async Task SendTradeOpenFailedAsync(
        PlayerId requesterId,
        PlayerId otherId,
        CancellationToken ct
    )
    {
        string otherName =
            otherId > 0
                ? await _grainFactory
                    .GetPlayerDirectoryGrain()
                    .GetPlayerNameAsync(otherId, ct)
                    .ConfigureAwait(true)
                : string.Empty;

        await _grainFactory
            .GetPlayerPresenceGrain(requesterId)
            .SendComposerAsync(
                new TradeOpenFailedEventPaserMessageComposer
                {
                    Reason = TradeOpenFailedReason,
                    OtherUserName = otherName,
                }
            )
            .ConfigureAwait(true);
    }

    private Task SendTradingNotOpenAsync(PlayerId playerId, CancellationToken ct) =>
        _grainFactory
            .GetPlayerPresenceGrain(playerId)
            .SendComposerAsync(new TradingNotOpenEventMessageComposer());

    private async Task BroadcastItemListAsync(RoomTradeSession session, CancellationToken ct)
    {
        ImmutableArray<FurnitureItemSnapshot> oneItems = await ResolveOfferSnapshotsAsync(
                session.UserOneId,
                session.UserOneItemIds,
                ct
            )
            .ConfigureAwait(true);
        ImmutableArray<FurnitureItemSnapshot> twoItems = await ResolveOfferSnapshotsAsync(
                session.UserTwoId,
                session.UserTwoItemIds,
                ct
            )
            .ConfigureAwait(true);

        await SendToTradeAsync(
                session,
                new TradingItemListEventMessageComposer
                {
                    FirstUserId = session.UserOneObjectId,
                    FirstUserItems = oneItems,
                    FirstUserCredits = 0,
                    SecondUserId = session.UserTwoObjectId,
                    SecondUserItems = twoItems,
                    SecondUserCredits = 0,
                }
            )
            .ConfigureAwait(true);
    }

    private async Task<ImmutableArray<FurnitureItemSnapshot>> ResolveOfferSnapshotsAsync(
        PlayerId playerId,
        List<int> itemIds,
        CancellationToken ct
    )
    {
        if (itemIds.Count == 0)
        {
            return [];
        }

        IInventoryGrain inventory = _grainFactory.GetInventoryGrain(playerId);
        ImmutableArray<FurnitureItemSnapshot>.Builder builder =
            ImmutableArray.CreateBuilder<FurnitureItemSnapshot>(itemIds.Count);

        foreach (int itemId in itemIds)
        {
            FurnitureItemSnapshot? snapshot = await inventory
                .GetItemSnapshotAsync(itemId, ct)
                .ConfigureAwait(true);

            if (snapshot is not null)
            {
                builder.Add(snapshot);
            }
        }

        return builder.ToImmutable();
    }

    private Task SendToTradeAsync(RoomTradeSession session, IComposer composer) =>
        Task.WhenAll(
            _grainFactory.GetPlayerPresenceGrain(session.UserOneId).SendComposerAsync(composer),
            _grainFactory.GetPlayerPresenceGrain(session.UserTwoId).SendComposerAsync(composer)
        );
}
