using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Game.Lobby;

/// <summary>
/// Clicking a resolution statue, and also picking an achievement in the dialog it opens — the
/// client sends the same message for both, with <see cref="AchievementId"/> as the difference.
/// </summary>
public record GetResolutionAchievementsMessage : IMessageEvent
{
    /// <summary>The statue's room-object id. The client calls it the "stuff id" throughout.</summary>
    public required int StuffId { get; init; }

    /// <summary>The achievement being picked, or zero when the client is only asking what the
    /// statue currently shows (opening it, or refreshing after a level-up).</summary>
    public required int AchievementId { get; init; }
}
