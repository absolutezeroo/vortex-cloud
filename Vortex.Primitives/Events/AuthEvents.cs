using System.Collections.Immutable;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Events;

/// <summary>A player successfully authenticated (SSO ticket validated).</summary>
public sealed record PlayerLoggedInEvent(int PlayerId, string? IpHash = null) : IEvent;

/// <summary>An authentication attempt failed (unknown or expired SSO ticket).</summary>
public sealed record PlayerLoginFailedEvent(string? IpHash = null) : IEvent;

/// <summary>
/// An account's password was written. <paramref name="StaffReset"/> separates the account proving
/// itself from an administrator taking responsibility for it -- the same write, but not the same
/// story when the account is later disputed.
/// </summary>
/// <remarks>
/// An account can hold several characters, and the audit is searched by character, so the players
/// are carried here and the handler writes one record each. Otherwise a password change would be
/// invisible from every profile it actually protects.
/// </remarks>
public sealed record AccountPasswordChangedEvent(
    int AccountId,
    ImmutableArray<int> PlayerIds,
    bool StaffReset,
    int RevokedSessions
) : IEvent;

/// <summary>The account's second factor was switched on or off.</summary>
public sealed record AccountMfaChangedEvent(
    int AccountId,
    ImmutableArray<int> PlayerIds,
    bool Enabled
) : IEvent;

/// <summary>A player claimed a new name; the old one is kept because that is what a report names.</summary>
public sealed record PlayerNameChangedEvent(PlayerId PlayerId, string OldName, string NewName)
    : IEvent;
