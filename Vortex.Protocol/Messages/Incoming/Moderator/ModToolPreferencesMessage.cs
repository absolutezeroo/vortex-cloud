using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

/// <summary>Where the moderator moved or resized their mod-tool window, so it can be restored at
/// the next login.</summary>
public record ModToolPreferencesMessage : IMessageEvent
{
    public required int WindowX { get; init; }
    public required int WindowY { get; init; }
    public required int WindowWidth { get; init; }
    public required int WindowHeight { get; init; }
}
