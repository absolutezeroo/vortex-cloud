using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

/// <summary>Keyed by user despite the name — the client's RoomVisitsCtrl asks "which rooms has this
/// person been in", not "who visited this room".</summary>
public record GetRoomVisitsMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
