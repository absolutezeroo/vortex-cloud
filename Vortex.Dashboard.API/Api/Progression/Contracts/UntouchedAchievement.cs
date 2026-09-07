namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>An achievement nobody has ever progressed. The triggered flag says why.</summary>
public sealed record UntouchedAchievement(
    int Id,
    string Name,
    string Category,
    bool Triggered,
    int Levels
);
