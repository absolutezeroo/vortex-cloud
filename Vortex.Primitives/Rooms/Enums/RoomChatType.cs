namespace Vortex.Primitives.Rooms.Enums;

/// <summary>
/// Which of the client's three chat packets a line is delivered on. The client draws a different
/// bubble for each, so a shout sent as <see cref="Chat"/> reaches everyone in the room looking like
/// an ordinary line.
/// </summary>
public enum RoomChatType
{
    Chat = 0,
    Shout = 1,
    Whisper = 2,
}
