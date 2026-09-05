using Orleans;

namespace Vortex.Primitives.Habbicons.Snapshots;

/// <summary>
/// A Habbicon as one player sees it in the shop: the definition, plus that player's state. This is
/// the shape the wire wants (<c>habbiconId, name, collectionId, state, priceCredits,
/// priceActivityPoints, activityPointType</c>).
/// </summary>
[GenerateSerializer, Immutable]
public sealed record HabbiconShopItemSnapshot
{
    [Id(0)]
    public required int HabbiconId { get; init; }

    [Id(1)]
    public required string Code { get; init; }

    [Id(2)]
    public required int CollectionId { get; init; }

    [Id(3)]
    public required HabbiconState State { get; init; }

    [Id(4)]
    public required int PriceCredits { get; init; }

    [Id(5)]
    public required int PriceActivityPoints { get; init; }

    [Id(6)]
    public required int ActivityPointType { get; init; }
}
