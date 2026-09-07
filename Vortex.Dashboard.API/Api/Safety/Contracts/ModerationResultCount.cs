namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>How many actions ended one way, named by the enum.</summary>
public sealed record ModerationResultCount(string Result, int Count);
