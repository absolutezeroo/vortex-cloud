using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Games;
using Vortex.Rooms.Games.Scoring;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Events;

/// <summary>
/// Something worth knowing happened in a match. These are the seam between game rules and everything
/// that reacts to them — scoreboards, avatar effects, the wired bridge, diagnostics — so that no
/// rule builds a packet and no presentation code decides anything.
/// <para>
/// Only meaningful state changes belong here. There is deliberately no event for every method call:
/// a tile repaint is presentation, not an event; a tile changing owner is.
/// </para>
/// </summary>
public abstract record GameEvent
{
    /// <summary>Stamped by the runtime as the event is published — a module raising an event says
    /// what happened, never which match it is in.</summary>
    public GameId Game { get; init; }

    /// <inheritdoc cref="Game"/>
    public MatchId Match { get; init; }
}

/// <summary>A match moved between lifecycle phases. Every transition raises exactly one of these,
/// which makes a broken match reconstructable from the log alone.</summary>
public sealed record GamePhaseChangedEvent : GameEvent
{
    public required GamePhase From { get; init; }

    public required GamePhase To { get; init; }
}

/// <summary>A match is live. Raised once, on entry to <see cref="GamePhase.Running"/>.</summary>
public sealed record GameMatchStartedEvent : GameEvent
{
    public required int Round { get; init; }
}

/// <summary>A match is over, with its final tally. Raised while the scores and rosters still stand,
/// so a high-score board records what actually happened rather than what is left a moment later.</summary>
public sealed record GameMatchEndedEvent : GameEvent
{
    public required MatchOutcome Outcome { get; init; }
}

/// <summary>A team's score changed, and why.</summary>
public sealed record GameScoreChangedEvent : GameEvent
{
    public required GameScore Score { get; init; }

    public required int PreviousTotal { get; init; }

    public required int NewTotal { get; init; }
}

/// <summary>A player joined a team (through a gate, a wired box or a balancing pick).</summary>
public sealed record GameParticipantJoinedEvent : GameEvent
{
    public required PlayerId Player { get; init; }

    public required TeamId Team { get; init; }
}

/// <summary>A player left a team, a match or the room.</summary>
public sealed record GameParticipantLeftEvent : GameEvent
{
    public required PlayerId Player { get; init; }

    public required TeamId Team { get; init; }
}

/// <summary>A player is out of the current match and cannot act as a participant again until the
/// next one. Distinct from leaving: an eliminated player is still in the room.</summary>
public sealed record GameParticipantEliminatedEvent : GameEvent
{
    public required PlayerId Player { get; init; }

    public required TeamId Team { get; init; }
}

/// <summary>The arena stopped being playable mid-match — the last goal was picked up, every arena
/// tile was removed. The runtime ends the match on this rather than letting it run on a rink that no
/// longer exists.</summary>
public sealed record GameArenaInvalidatedEvent : GameEvent
{
    public required string Reason { get; init; }
}

/// <summary>
/// The outcome of a finished match, built the moment it ends — while the final scores are standing
/// and the participants are still in their teams. Membership read any later is already shrinking as
/// players walk away.
/// <para>
/// Keyed by the game's own teams, not by colour. A Habbo high-score board needs colours; projecting
/// this outcome onto them is the presentation layer's job, and doing it there is what lets a game
/// with five teams — or with teams no coloured furni can show — still finish and still be recorded.
/// </para>
/// </summary>
public sealed record MatchOutcome
{
    /// <summary>The leading team, or <see cref="TeamId.None"/> on a scoreless match.</summary>
    public required TeamId WinningTeam { get; init; }

    /// <summary>Final score per team, zeros included.</summary>
    public required IReadOnlyDictionary<TeamId, int> Scores { get; init; }

    /// <summary>The display names of each team's members at the final whistle.</summary>
    public required IReadOnlyDictionary<TeamId, IReadOnlyList<string>> MemberNames { get; init; }
}

/// <summary>
/// Something that reacts to game events. Sinks are attached once per room by the runtime and run
/// inside the room's turn; a sink that throws is logged and skipped, because a broken scoreboard
/// must not be able to abort a match.
/// </summary>
public interface IGameEventSink
{
    Task OnGameEventAsync(GameEvent evt, CancellationToken ct);
}
