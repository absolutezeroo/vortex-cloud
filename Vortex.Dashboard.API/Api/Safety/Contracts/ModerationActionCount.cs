namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>How often one moderation action was taken.</summary>
public sealed record ModerationActionCount(string Action, int Count);
