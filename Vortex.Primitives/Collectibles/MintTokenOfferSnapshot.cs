using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>A bundle of mint tokens for sale, priced in silver.</summary>
[GenerateSerializer, Immutable]
public sealed record MintTokenOfferSnapshot
{
    [Id(0)]
    public required int OfferId { get; init; }

    [Id(1)]
    public required string ProductCode { get; init; }

    [Id(2)]
    public required int SilverPrice { get; init; }

    [Id(3)]
    public required int AmountTokens { get; init; }
}
