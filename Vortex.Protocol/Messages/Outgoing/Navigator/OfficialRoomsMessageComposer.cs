using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.Protocol.Messages.Outgoing.Navigator;

/// <summary>
/// The public/official rooms view. Both the old and the new navigator listen for this, so it is
/// still live on WIN63 — it was previously an <c>object?</c> placeholder with an empty serializer
/// body, and nothing ever constructed it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record OfficialRoomsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<OfficialRoomEntrySnapshot> Entries { get; init; }

    /// <summary>Optional hotel-wide promoted entry shown above the list. Null writes the "absent"
    /// marker; the client then reads no entry at all.</summary>
    [Id(1)]
    public OfficialRoomEntrySnapshot? AdRoom { get; init; }

    [Id(2)]
    public ImmutableArray<PromotedRoomCategorySnapshot> PromotedRooms { get; init; } = [];
}
