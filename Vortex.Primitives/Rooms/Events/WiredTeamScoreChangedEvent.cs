using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Primitives.Rooms.Events;

/// <summary>Raised when a wired game team's score changes, carrying the team plus its previous and new
/// totals. Drives the wired SCORE_ACHIEVED trigger, which fires once as the score crosses up onto a
/// configured threshold (the previous/new pair lets it fire exactly on the crossing, not every point
/// while already above it).</summary>
public sealed record WiredTeamScoreChangedEvent : RoomEvent
{
    public required GameTeamColor Team { get; init; }
    public required int Score { get; init; }
    public required int PreviousScore { get; init; }
}
