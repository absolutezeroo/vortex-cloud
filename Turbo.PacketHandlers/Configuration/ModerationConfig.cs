namespace Turbo.PacketHandlers.Configuration;

public sealed class ModerationConfig
{
    public const string SECTION_NAME = "Turbo:Moderation";

    /// <summary>
    /// The staff CFH tool's mute action carries no duration on the wire (unlike the in-room mute
    /// panel, which sends explicit minutes) — this is the server-side default applied for it.
    /// </summary>
    public int ModToolDefaultMuteDurationMinutes { get; init; } = 60;
}
