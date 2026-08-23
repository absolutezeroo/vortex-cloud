using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.Protocol.Messages.Outgoing.Navigator;

/// <summary>
/// The result of a guest-room search (header 160) - the query that produced it, the rooms it found,
/// and an optional promoted room shown alongside them.
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2064/_SafeCls_4150.as), which delegates to the DTO
/// in unknowns/_SafePkg_2008/_SafeCls_3104.as: an int, a string, a counted list of rooms in the
/// same layout GetGuestRoomResult uses, then a guard and the optional ad entry.
///
/// This was an <c>object?</c> placeholder with an empty serializer body, and nothing constructs it
/// yet; the shape is filled so that whoever wires the search up does not have to rediscover it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuestRoomSearchResultMessageComposer : IComposer
{
    /// <summary>Which search produced this result; echoed back so the client can match it to the
    /// view that asked.</summary>
    [Id(0)]
    public required int SearchType { get; init; }

    [Id(1)]
    public required string SearchParam { get; init; }

    [Id(2)]
    public required ImmutableArray<RoomInfoSnapshot> Rooms { get; init; }

    /// <summary>Optional promoted entry. Null writes the "absent" marker and the client then reads
    /// no entry at all.</summary>
    [Id(3)]
    public OfficialRoomEntrySnapshot? Ad { get; init; }
}
