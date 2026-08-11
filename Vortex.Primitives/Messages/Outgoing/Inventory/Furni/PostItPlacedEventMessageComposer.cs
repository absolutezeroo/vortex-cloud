using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Furni;

/// <summary>
/// Tells the client how many sheets a post-it stack has left after one was placed (header 2145).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2514/_SafeCls_2620.as): two ints, the inventory
/// item id and the remaining count. The client feeds them to
/// <c>FurniModel.updatePostItCount()</c>, which rewrites the count inside the item's stuff data.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PostItPlacedEventMessageComposer : IComposer
{
    /// <summary>The inventory item id of the post-it stack.</summary>
    [Id(0)]
    public required int ItemId { get; init; }

    /// <summary>Sheets remaining on that stack.</summary>
    [Id(1)]
    public required int ItemsLeft { get; init; }
}
