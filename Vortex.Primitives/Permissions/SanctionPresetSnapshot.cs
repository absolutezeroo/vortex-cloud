namespace Vortex.Primitives.Permissions;

/// <summary>Null <see cref="DurationSeconds"/> means permanent.</summary>
public readonly record struct SanctionPresetSnapshot(
    string Name,
    int? DurationSeconds,
    string? Message
);
