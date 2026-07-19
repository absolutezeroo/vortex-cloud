using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Moderation;

/// <summary>One room's worth of chat, for the staff mod tool's chatlog views. Matches the WIN63
/// client's chatlog block shape (room id/name context + a chat record list) — see
/// GetRoomChatlogMessageHandler/GetUserChatlogMessageHandler for the wire serialization.</summary>
[GenerateSerializer, Immutable]
public sealed record ChatlogBlockSnapshot
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required string RoomName { get; init; }

    [Id(2)]
    public required ImmutableArray<ChatlogRecordSnapshot> Records { get; init; }
}
