namespace Turbo.Primitives.Events;

/// <summary>A player successfully authenticated (SSO ticket validated).</summary>
public sealed record PlayerLoggedInEvent(int PlayerId, string? IpHash = null) : IEvent;

/// <summary>An authentication attempt failed (unknown or expired SSO ticket).</summary>
public sealed record PlayerLoginFailedEvent(string? IpHash = null) : IEvent;
