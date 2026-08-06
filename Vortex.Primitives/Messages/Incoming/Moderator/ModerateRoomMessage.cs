using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

/// <summary>The room-tool checkboxes, applied in one go. All three can be set at once.</summary>
public record ModerateRoomMessage : IMessageEvent
{
    public required int RoomId { get; init; }

    /// <summary>Force the room back to open-door, undoing a lock used to trap visitors.</summary>
    public bool LockDoor { get; init; }

    /// <summary>Reset an offensive room name and description to a neutral placeholder.</summary>
    public bool ChangeName { get; init; }

    public bool KickUsers { get; init; }
}
