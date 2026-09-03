using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Runtime;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The rules that decide WHICH arena a start or stop applies to, over hand-built candidates and no
/// room at all.
/// <para>
/// This is the replacement for "start every game whose arena validates". The old behaviour meant a
/// hall with a Banzai board, a Freeze rink and a football pitch answered one press of one counter by
/// kicking off three unrelated matches; the invariant now is that a request resolves to exactly one
/// arena or to none, and never to several.
/// </para>
/// </summary>
public sealed class GameTargetResolverTests
{
    private static readonly GameId Banzai = new("banzai");
    private static readonly GameId Freeze = new("freeze");

    private static ArenaCandidate Candidate(
        GameId game,
        int instance,
        int distance = int.MaxValue,
        params int[] components
    )
    {
        List<RoomObjectId> ids = [];

        foreach (int id in components)
        {
            ids.Add(new RoomObjectId(id));
        }

        return new ArenaCandidate(new ArenaId(game, instance), ids, distance);
    }

    [Fact]
    public void NoCandidates_ResolvesToNothing()
    {
        GameTarget target = GameTargetResolver.Resolve([], GameId.None, default);

        target.Outcome.Should().Be(GameTargetOutcome.NoCandidate);
        target.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void OneCandidate_Resolves_EvenWithNothingToGoOn()
    {
        // The ordinary room: one arena, a bare counter. Nothing to disambiguate, so nothing to refuse.
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0)],
            GameId.None,
            default
        );

        target.IsResolved.Should().BeTrue();
        target.Arena.Should().Be(new ArenaId(Banzai, 0));
        target.Reason.Should().Be(GameTargetReason.OnlyCandidate);
    }

    [Fact]
    public void SeveralCandidatesAndNothingToGoOn_IsAmbiguous_NotAGuessAndNotAFanOut()
    {
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0), Candidate(Freeze, 0)],
            GameId.None,
            default
        );

        target.Outcome.Should().Be(GameTargetOutcome.Ambiguous);
        target.IsResolved.Should().BeFalse();
        target.CandidateCount.Should().Be(2);
    }

    [Fact]
    public void NamingTheGame_PicksIt()
    {
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0), Candidate(Freeze, 0)],
            Freeze,
            default
        );

        target.Arena.Should().Be(new ArenaId(Freeze, 0));
        target.Reason.Should().Be(GameTargetReason.Explicit);
    }

    [Fact]
    public void NamingAGameWithTwoInstallations_IsStillAmbiguous()
    {
        // Naming a game narrows the field; it does not licence a guess between two of its boards.
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0), Candidate(Banzai, 1)],
            Banzai,
            default
        );

        target.Outcome.Should().Be(GameTargetOutcome.Ambiguous);
    }

    [Fact]
    public void NamingAGameTheRoomCannotPlay_ResolvesToNothing()
    {
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0)],
            Freeze,
            default
        );

        target.Outcome.Should().Be(GameTargetOutcome.NoCandidate);
    }

    [Fact]
    public void ASourceThatIsPartOfAnArena_PicksThatArena()
    {
        // A counter that is itself one of the arena's components is exact and free — no geometry.
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0, 9, 100, 101), Candidate(Freeze, 0, 1, 200)],
            GameId.None,
            new RoomObjectId(101)
        );

        target.Arena.Should().Be(new ArenaId(Banzai, 0));
        target.Reason.Should().Be(GameTargetReason.SourceIsComponent);
    }

    [Fact]
    public void ASourceNearerOneArena_PicksTheNearest()
    {
        // The counter beside the Banzai board starts Banzai. Generic geometry, no game named.
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0, distance: 2, 100), Candidate(Freeze, 0, distance: 9, 200)],
            GameId.None,
            new RoomObjectId(500)
        );

        target.Arena.Should().Be(new ArenaId(Banzai, 0));
        target.Reason.Should().Be(GameTargetReason.SourceIsNearest);
    }

    [Fact]
    public void ASourceEquallyNearTwoArenas_IsAmbiguous()
    {
        // A tie is not a decision. A room owner who puts one counter exactly between two boards gets
        // nothing rather than a coin flip.
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0, distance: 3, 100), Candidate(Freeze, 0, distance: 3, 200)],
            GameId.None,
            new RoomObjectId(500)
        );

        target.Outcome.Should().Be(GameTargetOutcome.Ambiguous);
    }

    [Fact]
    public void TwoInstallationsOfOneGame_AreToldApartByProximity()
    {
        // Two Banzai boards in a hall, a counter beside each: each counter starts its own board.
        GameTarget target = GameTargetResolver.Resolve(
            [Candidate(Banzai, 0, distance: 12, 100), Candidate(Banzai, 1, distance: 1, 200)],
            GameId.None,
            new RoomObjectId(500)
        );

        target.Arena.Should().Be(new ArenaId(Banzai, 1));
    }
}
