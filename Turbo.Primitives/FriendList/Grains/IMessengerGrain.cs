using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.Players;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.Primitives.FriendList.Grains;

public partial interface IMessengerGrain : IGrainWithIntegerKey
{
    // ── Reads ────────────────────────────────────────────────────────────────
    Task<List<MessengerFriendSnapshot>> GetFriendsAsync(CancellationToken ct);
    Task<List<FriendCategorySnapshot>> GetCategoriesAsync(CancellationToken ct);
    Task<List<int>> GetBlockedUserIdsAsync(CancellationToken ct);
    Task<List<int>> GetIgnoredUserIdsAsync(CancellationToken ct);
    Task<List<FriendRequestSnapshot>> GetFriendRequestsAsync(CancellationToken ct);
    Task<List<RelationshipStatusEntrySnapshot>> GetRelationshipStatusSummaryAsync(
        CancellationToken ct
    );

    // ── Predicate helpers ────────────────────────────────────────────────────
    Task<bool> IsFriendAsync(PlayerId targetId, CancellationToken ct);
    Task<bool> HasBlockedUserAsync(PlayerId targetId, CancellationToken ct);
    Task<bool> HasIgnoredUserAsync(PlayerId targetId, CancellationToken ct);

    // ── Friend request lifecycle ─────────────────────────────────────────────
    Task<FriendListErrorCodeType?> SendFriendRequestAsync(
        PlayerId targetId,
        string targetName,
        CancellationToken ct
    );
    Task ReceiveFriendRequestAsync(
        PlayerId requesterId,
        string requesterName,
        string requesterFigure,
        CancellationToken ct
    );
    Task<List<AcceptFriendFailureSnapshot>> AcceptFriendRequestsAsync(
        List<int> requesterIds,
        CancellationToken ct
    );
    Task DeclineFriendRequestsAsync(List<int> requesterIds, bool all, CancellationToken ct);

    // ── Friendship mutations ─────────────────────────────────────────────────
    Task<List<int>> RemoveFriendsAsync(List<int> friendIds, CancellationToken ct);
    Task NotifyFriendRemovedAsync(PlayerId removedBy, CancellationToken ct);
    Task SetRelationshipStatusAsync(
        int friendId,
        MessengerFriendRelationType relationType,
        CancellationToken ct
    );

    // ── Block / ignore ───────────────────────────────────────────────────────
    Task BlockUserAsync(PlayerId targetId, CancellationToken ct);
    Task UnblockUserAsync(PlayerId targetId, CancellationToken ct);
    Task IgnoreUserAsync(PlayerId targetId, CancellationToken ct);
    Task UnignoreUserAsync(PlayerId targetId, CancellationToken ct);

    // ── Messaging ────────────────────────────────────────────────────────────
    Task<InstantMessageErrorCodeType?> SendMessageAsync(
        PlayerId receiverId,
        string message,
        int chatId,
        int confirmationId,
        CancellationToken ct
    );
    Task ReceiveMessageAsync(
        PlayerId senderId,
        string senderName,
        string senderFigure,
        string message,
        DateTime timestamp,
        int messageId,
        CancellationToken ct
    );
    Task<List<MessageHistoryEntrySnapshot>> GetMessageHistoryAsync(
        PlayerId friendId,
        CancellationToken ct
    );

    // ── Presence notifications ───────────────────────────────────────────────
    Task NotifyOnlineAsync(CancellationToken ct);
    Task NotifyOfflineAsync(CancellationToken ct);
    Task NotifyFriendPresenceChangedAsync(
        PlayerId friendId,
        bool online,
        string figure,
        string motto,
        CancellationToken ct
    );

    // ── Room actions ─────────────────────────────────────────────────────────
    Task ReceiveRoomInviteAsync(PlayerId senderId, string message, CancellationToken ct);

    // ── Search ───────────────────────────────────────────────────────────────
    Task<List<MessengerSearchResultSnapshot>> GetFriendSearchResultsAsync(
        string query,
        CancellationToken ct
    );
}
