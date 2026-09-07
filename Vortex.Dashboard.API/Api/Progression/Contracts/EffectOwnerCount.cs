namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One effect and who owns it.</summary>
public sealed record EffectOwnerCount(
    int EffectId,
    string? ImageUrl,
    int Owners,
    int Activated,
    int Selected
);
