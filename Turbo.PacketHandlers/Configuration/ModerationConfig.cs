namespace Turbo.PacketHandlers.Configuration;

public sealed class ModerationConfig
{
    public const string SECTION_NAME = "Turbo:Moderation";

    /// <summary>
    /// The staff CFH tool's mute action carries no duration on the wire (unlike the in-room mute
    /// panel, which sends explicit minutes) — this is the server-side default applied for it.
    /// </summary>
    public int ModToolDefaultMuteDurationMinutes { get; init; } = 60;

    /// <summary>Max chat lines returned per room for GetRoomChatlogMessageHandler.</summary>
    public int RoomChatlogLimit { get; init; } = 100;

    /// <summary>Max distinct rooms returned for GetUserChatlogMessageHandler.</summary>
    public int UserChatlogRoomLimit { get; init; } = 10;

    /// <summary>Max chat lines per room for GetUserChatlogMessageHandler.</summary>
    public int UserChatlogMessagesPerRoom { get; init; } = 50;
}
