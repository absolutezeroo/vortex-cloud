namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One rung of the sanction ladder the mod tool offers.
/// </summary>
/// <param name="Permanent">The same fact as a null duration, spelled out.</param>
/// <param name="Message">What the player is told. Null for a preset that says nothing.</param>
public sealed record SanctionPresetRow(
    int Id,
    string Kind,
    int PresetIndex,
    string Name,
    int? DurationSeconds,
    string? Message,
    bool Permanent
);
