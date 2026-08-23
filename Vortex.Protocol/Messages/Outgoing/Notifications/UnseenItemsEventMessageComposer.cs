using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Notifications;

/// <summary>
/// Which inventory items the player has not looked at yet (header 3059) - what puts the dot on the
/// inventory tabs.
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1810/_SafeCls_2693.as): a count of categories, then
/// for each one its id and its own counted list of item ids.
///
/// This serializer was reachable only through the wrong map entry until 2026-08-12: the type was
/// registered against AccountPreferencesEventMessageComposerSerializer, which writes bytes of an
/// entirely different message and throws on the cast. Repairing that pairing is what exposed this
/// body as empty.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record UnseenItemsEventMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<UnseenItemCategory> Categories { get; init; }
}

/// <summary>One inventory category and the items in it the player has not seen.</summary>
[GenerateSerializer, Immutable]
public sealed record UnseenItemCategory
{
    [Id(0)]
    public required int CategoryId { get; init; }

    [Id(1)]
    public required ImmutableArray<int> ItemIds { get; init; }
}
