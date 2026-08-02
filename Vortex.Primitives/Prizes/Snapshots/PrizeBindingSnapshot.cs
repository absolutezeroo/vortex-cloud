using Orleans;

namespace Vortex.Primitives.Prizes.Snapshots;

/// <summary>What a furniture definition draws, and how much work it takes to get there.</summary>
[GenerateSerializer, Immutable]
public sealed record PrizeBindingSnapshot
{
    [Id(0)]
    public required string PoolCode { get; init; }

    /// <summary>Hits needed before the prize is handed out; never below one.</summary>
    [Id(1)]
    public required int HitsRequired { get; init; }
}
