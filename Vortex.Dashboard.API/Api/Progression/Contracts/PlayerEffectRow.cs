using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One effect a player holds.
/// </summary>
/// <param name="ImageUrl">Rendered on this player's own figure: an effect is only ever seen worn,
/// and worn by them is what the operator is checking.</param>
/// <param name="ActivatedAt">Null for an effect that has never been switched on -- which is what
/// separates an owned effect from a spent one.</param>
public sealed record PlayerEffectRow(
    int Id,
    int EffectId,
    string? ImageUrl,
    int SubType,
    int TotalDuration,
    DateTime? ActivatedAt,
    bool IsSelected
);
