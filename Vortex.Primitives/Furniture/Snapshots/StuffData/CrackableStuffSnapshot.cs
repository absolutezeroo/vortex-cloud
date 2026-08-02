using Orleans;

namespace Vortex.Primitives.Furniture.Snapshots.StuffData;

/// <summary>
/// Wire shape of a crackable furniture's data (format key 7). Field order is the contract with the
/// client's crackable parser, which reads state, then hits, then target.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CrackableStuffSnapshot : StuffDataSnapshot
{
    [Id(0)]
    public required string Data { get; init; }

    [Id(1)]
    public required int Hits { get; init; }

    [Id(2)]
    public required int Target { get; init; }
}
