namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>How many threads or posts sit in one moderation state, named by the enum.</summary>
public sealed record ForumStateCount(string State, int Count);
