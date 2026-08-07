using Orleans;

namespace Vortex.Primitives.Catalog.Snapshots;

/// <summary>
/// How the buyer chose to wrap a gift, straight off the purchase packet.
/// </summary>
/// <remarks>
/// Three separate ids, because the client draws three separate things: the stuff type picks which
/// <c>present_gen*</c> furniture the parcel is, while the box and ribbon are sprite layers on it,
/// packed into the item's <c>extra</c> field as <c>box * 1000 + ribbon</c>.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record GiftWrappingSpec
{
    [Id(0)]
    public required int StuffTypeId { get; init; }

    [Id(1)]
    public required int BoxTypeId { get; init; }

    [Id(2)]
    public required int RibbonTypeId { get; init; }

    /// <summary>The note the buyer typed, shown by the present's widget before it is opened.</summary>
    [Id(3)]
    public required string Message { get; init; }

    /// <summary>Whether the parcel names its sender. The buyer chooses; an anonymous gift shows no
    /// name and no figure.</summary>
    [Id(4)]
    public required bool ShowPurchaserName { get; init; }
}
