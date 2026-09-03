using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Games.Scoring;

namespace Vortex.Rooms.Games.Football;

/// <summary>Why football awarded points.</summary>
public static class FootballScoreReasons
{
    public static readonly ScoreReason Goal = new("football.goal");
}

/// <summary>
/// The ball went in. <see cref="Team"/> is the goal's own colour and the team credited;
/// <see cref="KickerTeam"/> is the team of whoever kicked it, so an own goal is visible here even
/// though the score itself cannot show one.
/// </summary>
public sealed record FootballGoalScoredEvent : GameEvent
{
    public required GameTeamColor Team { get; init; }

    public required PlayerId Kicker { get; init; }

    public required GameTeamColor KickerTeam { get; init; }

    public required RoomObjectId Goal { get; init; }
}

/// <summary>A ball was set moving. Raised on the kick rather than on each hop: a tile-by-tile event
/// would be twenty a second per ball and tells nobody anything the kick did not.</summary>
public sealed record FootballBallKickedEvent : GameEvent
{
    public required RoomObjectId Ball { get; init; }

    public required PlayerId Kicker { get; init; }

    public required Rotation Direction { get; init; }

    public required int Distance { get; init; }
}
