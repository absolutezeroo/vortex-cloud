namespace Turbo.Primitives.Events;

// Domain events for groups/guilds. Published via IEventPublisher; auto-discovered handlers in
// Turbo.Observability turn them into audit/metrics, and plugins observe them via IEventHandler/
// IEventBehavior. Keep them as the single source of truth for "what happened to a group".

/// <summary>A player created a guild (and was charged <paramref name="CreditCost"/> credits).</summary>
public sealed record GroupCreatedEvent(
    int ActorPlayerId,
    int GroupId,
    string GroupName,
    int RoomId,
    int CreditCost
) : IEvent;

/// <summary>A player joined an open guild directly (became a member).</summary>
public sealed record GroupMemberJoinedEvent(int ActorPlayerId, int GroupId) : IEvent;

/// <summary>A player requested membership of an exclusive guild (pending approval).</summary>
public sealed record GroupMembershipRequestedEvent(int ActorPlayerId, int GroupId) : IEvent;

/// <summary>An admin approved a pending membership request.</summary>
public sealed record GroupMembershipAcceptedEvent(
    int ActorPlayerId,
    int GroupId,
    int TargetPlayerId
) : IEvent;

/// <summary>An admin rejected a pending membership request.</summary>
public sealed record GroupMembershipRejectedEvent(
    int ActorPlayerId,
    int GroupId,
    int TargetPlayerId
) : IEvent;

/// <summary>An admin removed a member from the guild.</summary>
public sealed record GroupMemberKickedEvent(int ActorPlayerId, int GroupId, int TargetPlayerId)
    : IEvent;

/// <summary>An admin granted or revoked a member's admin rights.</summary>
public sealed record GroupMemberRankChangedEvent(
    int ActorPlayerId,
    int GroupId,
    int TargetPlayerId,
    bool IsAdmin
) : IEvent;

/// <summary>A guild's identity/colors/badge/settings were edited.</summary>
public sealed record GroupUpdatedEvent(int ActorPlayerId, int GroupId) : IEvent;

/// <summary>A guild was deactivated (dissolved) by its owner.</summary>
public sealed record GroupDeactivatedEvent(int ActorPlayerId, int GroupId) : IEvent;

/// <summary>A player set or cleared their favourite guild (null = cleared).</summary>
public sealed record GroupFavouriteChangedEvent(int ActorPlayerId, int? GroupId) : IEvent;

// ── Forums ──────────────────────────────────────────────────────────────────────

/// <summary>A new forum thread was opened.</summary>
public sealed record ForumThreadCreatedEvent(int ActorPlayerId, int GroupId, int ThreadId) : IEvent;

/// <summary>A post (reply) was added to a forum thread.</summary>
public sealed record ForumPostCreatedEvent(
    int ActorPlayerId,
    int GroupId,
    int ThreadId,
    int MessageId
) : IEvent;

/// <summary>A forum thread's state changed via moderation (lock/hide/restore).</summary>
public sealed record ForumThreadModeratedEvent(
    int ActorPlayerId,
    int GroupId,
    int ThreadId,
    int State
) : IEvent;

/// <summary>A forum post's state changed via moderation (hide/restore).</summary>
public sealed record ForumPostModeratedEvent(
    int ActorPlayerId,
    int GroupId,
    int MessageId,
    int State
) : IEvent;

/// <summary>A forum's permission settings were changed.</summary>
public sealed record ForumSettingsUpdatedEvent(int ActorPlayerId, int GroupId) : IEvent;
