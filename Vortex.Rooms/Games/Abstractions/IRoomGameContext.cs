using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Arena;
using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Games.Scoring;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Abstractions;

/// <summary>
/// The room, as a game module is allowed to see it. Every member here is something at least one
/// shipped game needs; nothing here hands out the room grain, so a game module cannot quietly reach
/// past this contract into the emulator, and a test can drive a whole match against a fake.
/// <para>
/// Everything runs inside the room grain's single-threaded turn. There is no locking anywhere in the
/// games tree and there must not be: the actor model already serialises the room, and a lock inside
/// it can only deadlock. What that guarantee does NOT give you is atomicity across an
/// <c>await</c> — the turn can interleave at every one — so a loop that awaits must iterate a
/// snapshot, which is why the arena's queries materialise.
/// </para>
/// </summary>
public interface IRoomGameContext
{
    RoomId RoomId { get; }

    ILogger Logger { get; }

    /// <summary>The room's shared team + score ledger. Read freely; write through
    /// <see cref="ScoreAsync"/> so the score event fires.</summary>
    GameTeamBook Teams { get; }

    IGameChrome Chrome { get; }

    /// <summary>This game's furniture, by capability.</summary>
    IGameArena Arena { get; }

    /// <summary>Seeded per match, so a replay of the same match makes the same rolls.</summary>
    IGameRandom Random { get; }

    /// <summary>This game's lifecycle phase. The single source of truth for "is my match live" —
    /// a module keeps no boolean of its own.</summary>
    GamePhase Phase { get; }

    /// <summary>The match being played, or <see cref="MatchId.None"/> when there is none.</summary>
    MatchId Match { get; }

    /// <summary>The timestamp of the room tick currently being processed.</summary>
    long NowMs { get; }

    /// <summary>
    /// Asks to be ticked on the next frame even with no match running. A game with no match is not
    /// ticked at all by default — at twenty frames a second per room, "return early when idle" is
    /// still twenty virtual calls a second per game in every room in the hotel — so a module with
    /// work in flight outside a match (a Banzai teleport mid-hop, a football rolling in a room with
    /// no goals) re-arms this each frame for as long as that work lasts.
    /// </summary>
    void KeepTicking();

    // --- tiles ------------------------------------------------------------

    int MapWidth { get; }

    bool InBounds(int x, int y);

    bool InBounds(int tileIdx);

    int ToIdx(int x, int y);

    (int X, int Y) ToXY(int tileIdx);

    Altitude TileHeight(int tileIdx);

    /// <summary>Whether a floor item could occupy that tile: in bounds, not disabled, nothing
    /// blocking a stack on it. What a rolling ball asks before taking its next step.</summary>
    bool IsTileOpenForItem(int tileIdx);

    bool HasAvatarOn(int tileIdx);

    /// <summary>Recomputes a tile's cached walkability. Needed when a component's own walkability
    /// changes with the match phase (a team gate closes while a match runs), because walkability is
    /// precomputed into the tile flags rather than asked of the logic per step.</summary>
    void RecomputeTile(int x, int y);

    /// <summary>The tile one step from <paramref name="tileIdx"/> in that direction, false at the map
    /// edge. The room's own tile arithmetic — a game never does index maths on the grid itself.</summary>
    bool TryGetTileInFront(int tileIdx, Rotation direction, out int nextTileIdx);

    // --- participants -----------------------------------------------------

    IReadOnlyList<PlayerId> PlayersOn(int tileIdx);

    bool TryGetPlayerTile(PlayerId playerId, out int tileIdx);

    bool TryGetPlayerPosition(PlayerId playerId, out int x, out int y);

    /// <summary>The direction the player's body faces, which while they are walking is the direction
    /// they are walking in — what decides which way a football they step on is kicked.</summary>
    bool TryGetPlayerFacing(PlayerId playerId, out Rotation facing);

    string? NameOf(PlayerId playerId);

    void CancelWalk(PlayerId playerId);

    /// <summary>Puts the player on that tile immediately and tells the room. Used for teleports and
    /// for moving an eliminated player out of the arena.</summary>
    Task MovePlayerAsync(PlayerId playerId, int tileIdx);

    /// <summary>Turns the player to face a coordinate and tells the room.</summary>
    Task FacePlayerAsync(PlayerId playerId, int targetX, int targetY);

    // --- furniture --------------------------------------------------------

    /// <summary>Slides a component's furni one hop to another tile, as a roller would: the room's
    /// authoritative position changes first, then the client is told to animate it.</summary>
    Task SlideItemAsync(IGameComponent component, int toTileIdx);

    // --- outward ----------------------------------------------------------

    /// <summary>Applies a scoring act and raises the score event. The only way a game changes a
    /// score — a direct write to the team book scores silently, which is what used to make a wired
    /// SCORE_ACHIEVED trigger miss half the points awarded in a match.</summary>
    Task ScoreAsync(GameScore score, CancellationToken ct);

    /// <summary>Publishes a domain event to the room's sinks (scoreboards, the wired bridge,
    /// diagnostics). The event's game and match ids are stamped by the runtime.</summary>
    Task PublishAsync(GameEvent evt, CancellationToken ct);

    /// <summary>Asks the runtime to end this game's match. A game NEVER ends itself: going through
    /// the runtime is what fires GAME_ENDS, resets the timer furni and unwinds the phases in order.</summary>
    Task RequestMatchEndAsync(CancellationToken ct);

    /// <summary>Resolves a whole balance group from server config in one grain round trip. Called at
    /// match start, which is the one place a round trip is affordable.</summary>
    Task<ImmutableDictionary<string, string>> GetConfigAsync(ImmutableArray<string> keys);
}
