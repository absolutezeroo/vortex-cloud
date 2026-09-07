namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>Who sends the most private messages in the window. The name is null for a deleted player.</summary>
public sealed record SocialSenderCount(int PlayerId, string? PlayerName, int Messages);
