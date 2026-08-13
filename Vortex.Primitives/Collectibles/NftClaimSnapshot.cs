using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>
/// One claim on the collectibles claims list — a token minted elsewhere that is waiting to be
/// pulled into the hotel.
///
/// Not the same struct as <see cref="CollectibleItemClaimSnapshot"/>, despite the name: that one is
/// four fields and rides inside the collections list. This one is twelve and stands alone.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftClaimSnapshot
{
    [Id(0)]
    public required string ClaimId { get; init; }

    [Id(1)]
    public required int Status { get; init; }

    [Id(2)]
    public required int ClaimedAmount { get; init; }

    [Id(3)]
    public required int ClaimLimit { get; init; }

    // Four longs in a row, and the client exposes them as Numbers with no call site that formats
    // them, so the units are not recoverable from the client. They are written as whatever the
    // server stores; nothing here interprets them.

    [Id(4)]
    public required long ValidFrom { get; init; }

    [Id(5)]
    public required long ValidTo { get; init; }

    [Id(6)]
    public required long CreatedAt { get; init; }

    [Id(7)]
    public required long UpdatedAt { get; init; }

    [Id(8)]
    public required string Collection { get; init; }

    [Id(9)]
    public required string ProductCode { get; init; }

    [Id(10)]
    public required string Wallet { get; init; }

    [Id(11)]
    public required NftClaimItemSnapshot ClaimItem { get; init; }
}

/// <summary>
/// The item a claim would hand over: the ordinary collectible product struct with two extra strings
/// after it. The client's class literally extends the product one, so the base fields come first
/// and in their usual order — including the amount that sits partway down rather than at the end.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftClaimItemSnapshot
{
    [Id(0)]
    public required CollectibleProductItemSnapshot Product { get; init; }

    [Id(1)]
    public required string SetId { get; init; }

    [Id(2)]
    public required string DefaultCollectionName { get; init; }
}
