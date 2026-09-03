using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Games;
using Vortex.Rooms.Games.Arena;

namespace Vortex.Rooms.Games.Abstractions;

/// <summary>
/// One playable game hosted by a room. A module implements only rules: it does not own its
/// lifecycle, its teams, its scores, its arena index or any packet. The runtime drives the phases,
/// the team book holds the teams, the arena answers "what furniture do I have", and the presentation
/// layer turns events into messages.
/// <para>
/// The contract is deliberately shaped so that adding a game is a new folder and a
/// <c>[RoomGame]</c> attribute — no core file changes, no registration line in the room grain, and
/// nothing that starts or stops a match has to learn the game's name.
/// </para>
/// <para>
/// Every method runs inside the room grain's single-threaded turn, so no locking. A method that
/// throws is logged and contained by the runtime — one broken game must not stop the room's others
/// from starting, ending or cleaning up — but that is a backstop, not a strategy.
/// </para>
/// </summary>
public interface IRoomGame
{
    /// <summary>The game's static shape: id, teams, rounds, phase timings, tick appetite.</summary>
    GameProfile Profile { get; }

    /// <summary>Whether the room currently holds enough of this game's furniture to play. Called
    /// before every match; a fatal shortfall refuses the start and says exactly what is missing
    /// instead of producing a match that silently does nothing.</summary>
    ArenaValidation ValidateArena();

    /// <summary>A match is being set up: reset arena furniture, build the roster, resolve balance
    /// config. The scores are already cleared and the teams still hold whoever picked a gate.</summary>
    Task OnPreparingAsync(GameMatch match, CancellationToken ct);

    /// <summary>The rules are live from here.</summary>
    Task OnStartedAsync(GameMatch match, CancellationToken ct);

    /// <summary>The round is over. Show the outcome; scoring is already closed. The runtime holds
    /// this phase for <c>GameProfile.RoundEndMs</c> before finishing or starting the next round.</summary>
    Task OnRoundEndingAsync(GameMatch match, CancellationToken ct);

    /// <summary>Cleanup, always called exactly once per match even when the match was refused
    /// mid-flight: drop deferred work, clear effects, thaw whoever is frozen, restore furniture.
    /// After this the game must hold nothing from the match that just ended.</summary>
    Task OnResettingAsync(GameMatch match, CancellationToken ct);

    /// <summary>A room tick. Called in every phase but <see cref="GamePhase.Idle"/>, and in that one
    /// only for a game that asked through <c>IRoomGameContext.KeepTicking</c>.</summary>
    Task TickAsync(long nowMs, CancellationToken ct);

    /// <summary>Something happened to one of this game's components. The only path from furniture
    /// into rules.</summary>
    Task OnSignalAsync(GameSignal signal, CancellationToken ct);

    /// <summary>A player left the room. Their team membership is already cleared; drop whatever else
    /// was held for them.</summary>
    Task OnParticipantLeftAsync(PlayerId playerId, CancellationToken ct);

    /// <summary>A player entered the room. Team auras re-sync on their own through the avatar
    /// snapshot; this is for game-specific entry state.</summary>
    Task OnParticipantEnteredAsync(PlayerId playerId, CancellationToken ct);
}
