using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>
/// The room's word filter (header 3208).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2918/_SafeCls_3620.as): a count then that many
/// strings. That parser returns false where every neighbouring one returns true; it changes nothing
/// on this side, but it is why the client's port looks wrong and is not.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomFilterSettingsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<string> BadWords { get; init; }
}
