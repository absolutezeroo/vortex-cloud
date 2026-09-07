namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One prize pool.
/// </summary>
/// <param name="Variants">Which variants this pool distinguishes, as the pool stores them.</param>
/// <param name="IsBuiltIn">The mystery box and trophy pools, which the emulator draws from by name
/// -- renaming or deleting one silently stops those features paying out.</param>
public sealed record PrizePoolRow(
    int Id,
    string Code,
    string Name,
    string? Variants,
    string? Notes,
    bool Enabled,
    bool IsBuiltIn
);
