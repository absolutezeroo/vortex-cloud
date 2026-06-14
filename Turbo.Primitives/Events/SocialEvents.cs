namespace Turbo.Primitives.Events;

/// <summary>A player accepted an incoming friend request (a social-graph edge was created).</summary>
public sealed record FriendRequestAcceptedEvent(int ActorPlayerId, int TargetPlayerId) : IEvent;

/// <summary>A player removed a friend (a social-graph edge was deleted).</summary>
public sealed record FriendRemovedEvent(int ActorPlayerId, int TargetPlayerId) : IEvent;

/// <summary>A player blocked another user.</summary>
public sealed record UserBlockedEvent(int ActorPlayerId, int TargetPlayerId) : IEvent;

/// <summary>A player unblocked a previously blocked user.</summary>
public sealed record UserUnblockedEvent(int ActorPlayerId, int TargetPlayerId) : IEvent;
