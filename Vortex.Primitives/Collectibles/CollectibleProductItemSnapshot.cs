using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>
/// One collectible in a collection, and how many of it the viewer owns.
/// <para>
/// The client reads these fields in an order that is not the order they are declared in its own
/// getters: the amount arrives in the middle, through a <c>readAdditionalParams</c> hook the item
/// class overrides, between the score and the pet figure. Writing them in any other order shifts
/// every field after it.
/// </para>
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CollectibleProductItemSnapshot
{
    /// <summary>A short on the wire, not an int.</summary>
    [Id(0)]
    public required int ProductTypeId { get; init; }

    [Id(1)]
    public required string ItemTypeId { get; init; }

    /// <summary>What owning one of these is worth towards the collection's score.</summary>
    [Id(2)]
    public required int Score { get; init; }

    /// <summary>How many the viewer owns; zero is what an uncollected item looks like.</summary>
    [Id(3)]
    public int Amount { get; init; }

    /// <summary>Only meaningful for a pet collectible; empty otherwise.</summary>
    [Id(4)]
    public string PetFigureString { get; init; } = string.Empty;

    [Id(5)]
    public ImmutableArray<int> FigureSetIds { get; init; } = [];

    /// <summary>The furniture classname, which is how a collectible is matched to what a player owns.</summary>
    [Id(6)]
    public required string ProductCode { get; init; }

    [Id(7)]
    public string Rarity { get; init; } = string.Empty;
}
