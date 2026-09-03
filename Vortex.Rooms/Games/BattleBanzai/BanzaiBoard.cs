using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Games.Presentation;

namespace Vortex.Rooms.Games.BattleBanzai;

/// <summary>What stepping on a tile did.</summary>
public enum BanzaiMarkKind
{
    /// <summary>Nothing — off-board tile, locked tile, or no team.</summary>
    None = 0,

    /// <summary>Advanced the stepper's own claim (first or second step).</summary>
    Fill = 1,

    /// <summary>Stole a neutral or enemy unlocked tile back to first claim.</summary>
    Hijack = 2,

    /// <summary>Completed the third step — the tile locked, possibly enclosing a region.</summary>
    Lock = 3,
}

/// <summary>The full outcome of a step: the tile's new wire state, what happened, and any enclosed
/// region that locked with it (already applied to the board; the caller paints the furni).</summary>
public readonly record struct BanzaiMarkResult(
    BanzaiMarkKind Kind,
    int NewState,
    IReadOnlyList<int> RegionLocked
)
{
    public static readonly BanzaiMarkResult None = new(BanzaiMarkKind.None, 0, []);
}

/// <summary>
/// The pure Battle Banzai board: which tile indices are the arena, each tile's wire state, the
/// claim state machine and the enclosure flood fill. No IO — <see cref="BanzaiGame"/> turns the
/// returned changes into furni state broadcasts. Tile adjacency is 4-neighbour over the room
/// grid (the board knows the map width so index arithmetic cannot wrap across rows).
/// <para>
/// Rules verified against Arcturus (<c>InteractionBattleBanzaiTile</c>, <c>BattleBanzaiGame</c>):
/// stepping on your own claim advances it (t*3 → t*3+1 → t*3+2 locked); a locked tile is inert;
/// anything else (neutral or enemy claim) hijacks back to your first claim. Locking triggers a
/// flood fill from the locked tile's neighbours: a region bounded entirely by your locked tiles
/// locks wholesale, while a region that touches any non-board position leaks and locks nothing.
/// Arcturus locks only the LARGEST surviving region — mirrored here, pinned by a test; locking all
/// of them is the one-line switch in <see cref="LockEnclosedRegions"/> if fidelity is ever decided
/// the other way.
/// </para>
/// </summary>
public sealed class BanzaiBoard
{
    // Captured at Activate, NOT construction: the room model (and so the map width) is only loaded
    // when the grain activates, long after the systems are built.
    private int _mapWidth;
    private readonly Dictionary<int, int> _stateByTile = [];

    public bool IsActive { get; private set; }

    public IReadOnlyDictionary<int, int> States => _stateByTile;

    public int TileCount => _stateByTile.Count;

    /// <summary>Starts a round over these arena tiles — every tile lights up neutral.
    /// <paramref name="mapWidth"/> is the room grid width the adjacency math runs on.</summary>
    public void Activate(IEnumerable<int> tileIndices, int mapWidth)
    {
        _mapWidth = Math.Max(1, mapWidth);
        _stateByTile.Clear();

        foreach (int idx in tileIndices)
        {
            _stateByTile[idx] = BanzaiConstants.TileNeutral;
        }

        IsActive = _stateByTile.Count > 0;
    }

    /// <summary>Ends the round. Neutral tiles go dark; claimed and locked tiles keep their colour
    /// until the next round's <see cref="Activate"/> (Arcturus behaviour). Returns the tiles whose
    /// state changed so the caller can repaint them.</summary>
    public List<int> Deactivate()
    {
        IsActive = false;

        List<int> changed = [];

        foreach ((int idx, int state) in _stateByTile)
        {
            if (state == BanzaiConstants.TileNeutral)
            {
                changed.Add(idx);
            }
        }

        foreach (int idx in changed)
        {
            _stateByTile[idx] = BanzaiConstants.TileOff;
        }

        return changed;
    }

    /// <summary>Drops a tile from the board — its furni left the room mid-match. The arena shrinks
    /// rather than the match holding a reference to furniture that is gone, which is what makes
    /// "the owner picked up half the rink" a smaller board instead of a stuck match.</summary>
    public bool Remove(int tileIdx) => _stateByTile.Remove(tileIdx);

    public int StateOf(int tileIdx) =>
        _stateByTile.TryGetValue(tileIdx, out int state) ? state : BanzaiConstants.TileOff;

    /// <summary>Every tile locked by <paramref name="team"/> — what the winner flicker blinks.</summary>
    public List<int> LockedTilesOf(GameTeamColor team)
    {
        int locked = LockedStateOf(team);

        return [.. _stateByTile.Where(kv => kv.Value == locked).Select(kv => kv.Key)];
    }

    /// <summary>True when no tile is left unlocked — the early-end condition.</summary>
    public bool AllLocked()
    {
        if (_stateByTile.Count == 0)
        {
            return false;
        }

        foreach (int state in _stateByTile.Values)
        {
            if (!IsLockedState(state))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A team's first-claim wire state (t*3).</summary>
    public static int FirstClaimStateOf(GameTeamColor team) =>
        BanzaiConstants.TeamStateBase * (int)team;

    public static int LockedStateOf(GameTeamColor team) =>
        FirstClaimStateOf(team) + BanzaiConstants.LockedOffset;

    public static bool IsLockedState(int state) =>
        state >= BanzaiConstants.TeamStateBase
        && state % BanzaiConstants.TeamStateBase == BanzaiConstants.LockedOffset;

    /// <summary>Applies one step by <paramref name="team"/> onto <paramref name="tileIdx"/>.</summary>
    public BanzaiMarkResult Mark(GameTeamColor team, int tileIdx)
    {
        if (
            !IsActive
            || !HabboTeamPalette.IsColour(team)
            || !_stateByTile.TryGetValue(tileIdx, out int state)
            || state == BanzaiConstants.TileOff
            || IsLockedState(state)
        )
        {
            return BanzaiMarkResult.None;
        }

        int firstClaim = FirstClaimStateOf(team);

        // Advancing your own claim...
        if (state == firstClaim || state == firstClaim + 1)
        {
            int advanced = state + 1;
            _stateByTile[tileIdx] = advanced;

            if (!IsLockedState(advanced))
            {
                return new BanzaiMarkResult(BanzaiMarkKind.Fill, advanced, []);
            }

            return new BanzaiMarkResult(
                BanzaiMarkKind.Lock,
                advanced,
                LockEnclosedRegions(tileIdx, team)
            );
        }

        // ...or stealing neutral / enemy ground back to your first claim.
        _stateByTile[tileIdx] = firstClaim;

        return new BanzaiMarkResult(BanzaiMarkKind.Hijack, firstClaim, []);
    }

    /// <summary>
    /// The enclosure rule, run after <paramref name="lockedIdx"/> locked for <paramref name="team"/>:
    /// flood-fills (iteratively — a room-sized region must not recurse) from each neighbour through
    /// everything that is not already locked by the team. A fill that touches any non-board position
    /// leaks and dies; a fill bounded entirely by the team's locked tiles survives. Arcturus locks
    /// only the largest survivor — swap the <c>MaxBy</c> block for "lock every candidate" if the
    /// all-regions reading is ever preferred. Returns the region tiles now locked (state applied).
    /// </summary>
    private List<int> LockEnclosedRegions(int lockedIdx, GameTeamColor team)
    {
        int lockedState = LockedStateOf(team);
        List<HashSet<int>> candidates = [];
        HashSet<int> alreadyExplored = [];

        foreach (int seed in NeighborsOf(lockedIdx))
        {
            if (
                seed < 0
                || !_stateByTile.TryGetValue(seed, out int seedState)
                || seedState == lockedState
                || alreadyExplored.Contains(seed)
            )
            {
                continue;
            }

            HashSet<int> region = [seed];
            Queue<int> frontier = new();
            frontier.Enqueue(seed);
            bool leaked = false;

            while (frontier.TryDequeue(out int current))
            {
                foreach (int next in NeighborsOf(current))
                {
                    if (next < 0 || !_stateByTile.TryGetValue(next, out int nextState))
                    {
                        // Off the arena: this pocket is open to the world, nothing locks.
                        leaked = true;

                        continue;
                    }

                    if (nextState == lockedState || !region.Add(next))
                    {
                        continue;
                    }

                    frontier.Enqueue(next);
                }
            }

            alreadyExplored.UnionWith(region);

            if (!leaked)
            {
                candidates.Add(region);
            }
        }

        if (candidates.Count == 0)
        {
            return [];
        }

        // D-C2: Arcturus quirk — only the largest enclosed pocket locks.
        HashSet<int> winner = candidates.MaxBy(region => region.Count)!;

        foreach (int idx in winner)
        {
            _stateByTile[idx] = lockedState;
        }

        return [.. winner];
    }

    /// <summary>The 4-neighbours of a tile as indices, -1 where the map edge is — computed via x/y
    /// so an index at column 0 never "wraps" onto the previous row's last tile.</summary>
    private IEnumerable<int> NeighborsOf(int tileIdx)
    {
        int x = tileIdx % _mapWidth;

        yield return x > 0 ? tileIdx - 1 : -1;
        yield return x < _mapWidth - 1 ? tileIdx + 1 : -1;
        yield return tileIdx - _mapWidth; // negative when off the top — caller treats < 0 as off-board
        yield return tileIdx + _mapWidth;
    }
}
