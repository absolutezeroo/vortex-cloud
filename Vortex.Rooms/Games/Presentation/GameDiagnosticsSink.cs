using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Rooms.Games.Events;

namespace Vortex.Rooms.Games.Presentation;

/// <summary>
/// The trace that makes a broken match reconstructable. Every phase transition, score, elimination
/// and arena failure lands here with its room, game and match ids attached, so an operator reading
/// the log can follow one match from kick-off to result without any of it being inferred.
/// <para>
/// Nothing routine is logged above Debug, and no gameplay movement is logged at all: at twenty
/// frames a second per room, a line per step would drown the log it is meant to explain. The two
/// things that are always worth a line at Information — a match starting and a match ending — are
/// already covered by the phase transitions the runtime logs on refusal, so this sink stays at Debug
/// and is switched on per category when a room needs following.
/// </para>
/// </summary>
public sealed class GameDiagnosticsSink(ILogger logger) : IGameEventSink
{
    private readonly ILogger _logger = logger;

    public Task OnGameEventAsync(GameEvent evt, CancellationToken ct)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return Task.CompletedTask;
        }

        switch (evt)
        {
            case GamePhaseChangedEvent phase:
                _logger.LogDebug(
                    "[game {Game} match {Match}] {From} -> {To}",
                    evt.Game,
                    evt.Match,
                    phase.From,
                    phase.To
                );
                break;

            case GameScoreChangedEvent score:
                _logger.LogDebug(
                    "[game {Game} match {Match}] {Team} {Previous} -> {New} ({Amount} for "
                        + "{Reason} by player {Player}, furni {Source})",
                    evt.Game,
                    evt.Match,
                    score.Score.Team,
                    score.PreviousTotal,
                    score.NewTotal,
                    score.Score.Amount,
                    score.Score.Reason,
                    score.Score.Player,
                    score.Score.Source
                );
                break;

            case GameParticipantEliminatedEvent eliminated:
                _logger.LogDebug(
                    "[game {Game} match {Match}] player {Player} ({Team}) eliminated",
                    evt.Game,
                    evt.Match,
                    eliminated.Player,
                    eliminated.Team
                );
                break;

            case GameArenaInvalidatedEvent invalidated:
                _logger.LogDebug(
                    "[game {Game} match {Match}] arena invalidated: {Reason}",
                    evt.Game,
                    evt.Match,
                    invalidated.Reason
                );
                break;

            case GameMatchEndedEvent ended:
                _logger.LogDebug(
                    "[game {Game} match {Match}] finished, winner {Winner}",
                    evt.Game,
                    evt.Match,
                    ended.Outcome.WinningTeam
                );
                break;
        }

        return Task.CompletedTask;
    }
}
