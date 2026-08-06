using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Players;

/// <summary>Where a staff member last left their mod-tool window.</summary>
[GenerateSerializer, Immutable]
public sealed record PlayerModToolPreferencesSnapshot
{
    [Id(0)]
    public required int WindowX { get; init; }

    [Id(1)]
    public required int WindowY { get; init; }

    [Id(2)]
    public required int WindowWidth { get; init; }

    [Id(3)]
    public required int WindowHeight { get; init; }

    /// <summary>False when the player has never positioned the window, so the caller can skip
    /// pushing a rectangle of zeroes that would collapse the client's layout.</summary>
    [Id(4)]
    public bool IsSet { get; init; }
}
