using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// One playable game hosted by a room — Freeze today, Battle Banzai / football / hockey next. A game
/// registers itself with <see cref="RoomGameSystem"/> once in the room grain's constructor and gets the
/// four things every room game needs: told when a round starts and ends, a slice of the room tick, and
/// a chance to clean up after someone who walked out.
/// <para>
/// The point of the seam is that nothing outside has to know which games exist. The game-timer furni and
/// the wired control-clock action start "the room's game", not "the room's game and Freeze and football
/// and hockey" — adding the next game is a registration line, not an edit to every caller that can start
/// or stop a round. Teams and scores are NOT part of this interface: they live once in
/// <see cref="GameTeamState"/>, which the coordinator owns and every game shares, so the wired team
/// leaves read the same numbers whichever game is running.
/// </para>
/// <para>
/// Implementations run inside the room grain's single-threaded turn, so no locking. Every method must be
/// cheap and safe to call when the game is idle: a room with no arena furniture still ticks its games on
/// every frame, and a round it never joined still gets the start/end calls.
/// </para>
/// </summary>
public interface IRoomMinigame
{
    /// <summary>Short stable identifier, used to label this game's slice of the room tick in the tick
    /// metrics — keep it lowercase and constant.</summary>
    string Name { get; }

    /// <summary>A round is starting. The shared scores are already cleared and the wired GAME_STARTS
    /// trigger has already fired by the time this runs.</summary>
    Task StartAsync(CancellationToken ct);

    /// <summary>The round is over. The wired GAME_ENDS trigger has already fired, and the final scores
    /// are still intact so they can be read/displayed here.</summary>
    Task EndAsync(CancellationToken ct);

    /// <summary>One room tick. Called whether or not a round is running, so return early when idle.</summary>
    Task TickAsync(long nowMs, CancellationToken ct);

    /// <summary>A player left the room; drop whatever state was held for them.</summary>
    Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct);

    /// <summary>A player entered the room. Existing team auras already re-sync on their own — the
    /// avatar snapshot carries <c>CurrentEffectId</c> — so this hook is for game-specific entry work
    /// (roster re-entry policy, per-game HUD state), not effect replay.</summary>
    Task OnPlayerEnteredAsync(PlayerId playerId, CancellationToken ct);
}
