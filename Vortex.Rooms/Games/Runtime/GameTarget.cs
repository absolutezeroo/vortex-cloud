using System.Collections.Generic;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Games.Runtime;

/// <summary>Why a start or stop request resolved the way it did. Reported in the log and asserted in
/// tests, because "nothing happened" and "the wrong thing happened" must be told apart.</summary>
public enum GameTargetOutcome
{
    /// <summary>Exactly one arena was named or implied. <c>Arena</c> is it.</summary>
    Resolved = 0,

    /// <summary>The room holds no arena that could serve this request: no game furniture at all, or
    /// nothing valid to start / nothing running to stop.</summary>
    NoCandidate = 1,

    /// <summary>More than one arena could have served it and nothing distinguished them. The request
    /// is refused — starting "whichever" or, worse, starting all of them, is the bug this exists to
    /// prevent.</summary>
    Ambiguous = 2,
}

/// <summary>How the target was picked. Purely diagnostic, and the reason an operator can tell a
/// deliberate single-arena room from a lucky one.</summary>
public enum GameTargetReason
{
    None = 0,

    /// <summary>The caller named the game.</summary>
    Explicit = 1,

    /// <summary>The requesting furni is itself part of the arena.</summary>
    SourceIsComponent = 2,

    /// <summary>The requesting furni is nearer to this arena's footprint than to any other.</summary>
    SourceIsNearest = 3,

    /// <summary>It was the only candidate in the room.</summary>
    OnlyCandidate = 4,
}

/// <summary>The answer: which arena a start or stop applies to, and how that was decided.</summary>
public readonly record struct GameTarget(
    GameTargetOutcome Outcome,
    ArenaId Arena,
    GameTargetReason Reason,
    int CandidateCount
)
{
    public static GameTarget None(int candidates = 0) =>
        new(GameTargetOutcome.NoCandidate, ArenaId.None, GameTargetReason.None, candidates);

    public static GameTarget Ambiguous(int candidates) =>
        new(GameTargetOutcome.Ambiguous, ArenaId.None, GameTargetReason.None, candidates);

    public static GameTarget Resolved(ArenaId arena, GameTargetReason reason, int candidates) =>
        new(GameTargetOutcome.Resolved, arena, reason, candidates);

    public bool IsResolved => Outcome == GameTargetOutcome.Resolved;
}

/// <summary>One arena, as the resolver needs to see it: nothing but identity and geometry.</summary>
public readonly record struct ArenaCandidate(
    ArenaId Arena,
    IReadOnlyList<RoomObjectId> Components,
    int DistanceFromSource
);

/// <summary>
/// Picks the one arena a start or stop request applies to.
/// <para>
/// This exists because "start the game" used to mean "start every game whose arena validates", so a
/// hall with a Banzai board, a Freeze rink and a football pitch answered one press of one counter by
/// kicking off three unrelated matches. A start now has a target or it has nothing.
/// </para>
/// <para>The rules, in order, and none of them names a game:</para>
/// <list type="number">
/// <item><b>Explicit</b> — the caller said which game. An admin command, a wired box configured with
/// one, a test.</item>
/// <item><b>The source's own arena</b> — the requesting furni is one of that arena's components.
/// Exact, and free.</item>
/// <item><b>The arena the source is nearest to</b> — a counter beside the Banzai board starts Banzai.
/// Measured to the nearest tile of each footprint; a tie is not a decision and falls through.</item>
/// <item><b>The only candidate</b> — one startable arena in the room, so there is nothing to confuse.
/// This is what keeps every ordinary single-game room behaving exactly as it did.</item>
/// </list>
/// <para>
/// Anything else is <see cref="GameTargetOutcome.Ambiguous"/> and the request is refused. That is the
/// deliberate semantic for an ambiguous room: a room owner who builds two arenas and one bare counter
/// gets nothing rather than a coin flip, and the log says which arenas were in contention.
/// </para>
/// <para>Pure — a function of candidates and a request — so all of it is testable with no room.</para>
/// </summary>
public static class GameTargetResolver
{
    public static GameTarget Resolve(
        IReadOnlyList<ArenaCandidate> candidates,
        GameId explicitGame,
        RoomObjectId source
    )
    {
        if (candidates.Count == 0)
        {
            return GameTarget.None();
        }

        if (!explicitGame.IsNone)
        {
            return ResolveExplicit(candidates, explicitGame);
        }

        if (source.Value != 0 && TryResolveBySource(candidates, source, out GameTarget bySource))
        {
            return bySource;
        }

        return candidates.Count == 1
            ? GameTarget.Resolved(
                candidates[0].Arena,
                GameTargetReason.OnlyCandidate,
                candidates.Count
            )
            : GameTarget.Ambiguous(candidates.Count);
    }

    private static GameTarget ResolveExplicit(
        IReadOnlyList<ArenaCandidate> candidates,
        GameId game
    )
    {
        ArenaId found = ArenaId.None;
        int matches = 0;

        foreach (ArenaCandidate candidate in candidates)
        {
            if (candidate.Arena.Game != game)
            {
                continue;
            }

            matches++;
            found = candidate.Arena;
        }

        return matches switch
        {
            0 => GameTarget.None(candidates.Count),
            1 => GameTarget.Resolved(found, GameTargetReason.Explicit, candidates.Count),
            // The named game has two installations and the caller did not say which: still
            // ambiguous. Naming a game narrows the field; it does not licence a guess.
            _ => GameTarget.Ambiguous(matches),
        };
    }

    private static bool TryResolveBySource(
        IReadOnlyList<ArenaCandidate> candidates,
        RoomObjectId source,
        out GameTarget target
    )
    {
        foreach (ArenaCandidate candidate in candidates)
        {
            foreach (RoomObjectId component in candidate.Components)
            {
                if (component == source)
                {
                    target = GameTarget.Resolved(
                        candidate.Arena,
                        GameTargetReason.SourceIsComponent,
                        candidates.Count
                    );

                    return true;
                }
            }
        }

        int bestDistance = int.MaxValue;
        int bestCount = 0;
        ArenaId nearest = ArenaId.None;

        foreach (ArenaCandidate candidate in candidates)
        {
            if (candidate.DistanceFromSource < 0 || candidate.DistanceFromSource == int.MaxValue)
            {
                continue;
            }

            if (candidate.DistanceFromSource < bestDistance)
            {
                bestDistance = candidate.DistanceFromSource;
                nearest = candidate.Arena;
                bestCount = 1;
            }
            else if (candidate.DistanceFromSource == bestDistance)
            {
                bestCount++;
            }
        }

        if (bestCount == 1)
        {
            target = GameTarget.Resolved(
                nearest,
                GameTargetReason.SourceIsNearest,
                candidates.Count
            );

            return true;
        }

        target = default;

        return false;
    }
}
