using Orleans;

namespace Vortex.Primitives.Habbicons.Snapshots;

/// <summary>The outcome of asking to grant a Habbicon.</summary>
[GenerateSerializer, Immutable]
public readonly record struct HabbiconGrantResult
{
    /// <summary>False when the Habbicon does not exist or is not grantable.</summary>
    [Id(0)]
    public required bool Succeeded { get; init; }

    /// <summary>
    /// True when the player did not already own it. A repeat grant succeeds and reports false, which
    /// is what makes every grant path idempotent without its callers needing to check first.
    /// </summary>
    [Id(1)]
    public required bool WasNew { get; init; }

    [Id(2)]
    public required HabbiconState State { get; init; }

    public static HabbiconGrantResult Failed =>
        new()
        {
            Succeeded = false,
            WasNew = false,
            State = HabbiconState.NotOwned,
        };
}
