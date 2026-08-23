using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Game.Lobby;

/// <summary>
/// "Re-select achievement" on the progress view, behind a confirmation. Throws away the challenge
/// in progress on that statue so a new one can be picked.
/// </summary>
public record ResetResolutionAchievementMessage : IMessageEvent
{
    public required int StuffId { get; init; }
}
