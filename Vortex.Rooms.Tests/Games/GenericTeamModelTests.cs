using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Games.Teams;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The team model is the game's, not Habbo's. These pin the properties that the old
/// <c>GameTeamColor</c>-as-identity model could not have: a game with two teams, a game with seven, a
/// game whose teams have no colour at all — and, alongside them, that the four Habbo colours still map
/// exactly as they always did for the games whose arenas are built from coloured furniture.
/// </summary>
public sealed class GenericTeamModelTests
{
    private static PlayerId P(int id) => (PlayerId)id;

    [Fact]
    public void ATwoTeamGame_HasExactlyTwoTeams()
    {
        TeamSet duel = TeamSet.Of("attackers", "defenders");

        duel.Count.Should().Be(2);
        duel.Contains(new TeamId(1)).Should().BeTrue();
        duel.Contains(new TeamId(3)).Should().BeFalse("the set says two, so there is no third");
    }

    [Fact]
    public void AGameWithMoreThanFourTeams_Works()
    {
        // The number that used to be impossible. Nothing in the book counts to four any more.
        TeamSet seven = TeamSet.Of("a", "b", "c", "d", "e", "f", "g");
        TeamBook book = new(seven);

        for (int team = 1; team <= 7; team++)
        {
            book.JoinTeam(P(team), new TeamId(team)).Should().BeTrue();
            book.AddScore(new TeamId(team), team);
        }

        book.GetOccupiedTeamCount().Should().Be(7);
        book.GetTeamScore(new TeamId(7)).Should().Be(7);
        book.GetLeadingTeam().Should().Be(new TeamId(7));
    }

    [Fact]
    public void ColourlessTeams_ScoreAndWinLikeAnyOther()
    {
        TeamSet hunt = TeamSet.Of("hunters", "hiders");
        TeamBook book = new(hunt);

        book.JoinTeam(P(1), new TeamId(2));
        book.AddScore(new TeamId(2), 9);

        book.GetLeadingTeam().Should().Be(new TeamId(2));
        book.GetPlayersInTeam(new TeamId(2)).Should().ContainSingle();
    }

    [Fact]
    public void ColourlessTeams_HaveNoHabboColour_AndThatIsNotAnError()
    {
        HabboTeamPalette palette = HabboTeamPalette.For(TeamSet.Of("hunters", "hiders"));

        palette.IsComplete.Should().BeFalse("no coloured furni can present these teams");
        palette.ColourOf(new TeamId(1)).Should().Be(GameTeamColor.None);
        palette.TeamOf(GameTeamColor.Red).Should().Be(TeamId.None);
    }

    [Fact]
    public void TheHabboColours_MapOneToOneInWireOrder()
    {
        // The compatibility that every shipped game depends on: red is team 1 and effect base + 1,
        // exactly as it was when the colour WAS the team.
        HabboTeamPalette palette = HabboTeamPalette.Standard;

        palette.IsComplete.Should().BeTrue();
        palette.ColourOf(new TeamId(1)).Should().Be(GameTeamColor.Red);
        palette.ColourOf(new TeamId(2)).Should().Be(GameTeamColor.Green);
        palette.ColourOf(new TeamId(3)).Should().Be(GameTeamColor.Blue);
        palette.ColourOf(new TeamId(4)).Should().Be(GameTeamColor.Yellow);

        palette.TeamOf(GameTeamColor.Yellow).Should().Be(new TeamId(4));
        palette.TeamOf(GameTeamColor.None).Should().Be(TeamId.None);
    }

    [Fact]
    public void APartlyColouredGame_MapsWhatItCanAndLeavesTheRest()
    {
        // Five teams: four can be shown on coloured furniture, the fifth cannot. That is a
        // presentation limit and it now lives in the palette instead of in the shape of the model.
        HabboTeamPalette palette = HabboTeamPalette.For(
            TeamSet.Of("red", "green", "blue", "yellow", "purple")
        );

        palette.IsComplete.Should().BeFalse();
        palette.ColourOf(new TeamId(1)).Should().Be(GameTeamColor.Red);
        palette.ColourOf(new TeamId(5)).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void TwoTeamsClaimingOneColour_DoNotShareItsBoards()
    {
        HabboTeamPalette palette = HabboTeamPalette.For(TeamSet.Of("red", "red"));

        palette.ColourOf(new TeamId(1)).Should().Be(GameTeamColor.Red);
        palette
            .ColourOf(new TeamId(2))
            .Should()
            .Be(GameTeamColor.None, "the second must not silently steal the first team's boards");
    }

    [Fact]
    public void ASetThatRedeclaresTheRoomsTeams_IsTheSameTeamSpace()
    {
        // What decides whether an arena shares the room's Habbo ledger. Capacity is not part of it: a
        // game that caps the room's teams at three a side is still playing the room's teams.
        TeamSet capped = TeamSet.HabboColours.WithCapacity(3);

        capped.HasSameTeamsAs(TeamSet.HabboColours).Should().BeTrue();
        TeamSet.Of("red", "blue").HasSameTeamsAs(TeamSet.HabboColours).Should().BeFalse();
        TeamSet
            .Of("green", "red", "blue", "yellow")
            .HasSameTeamsAs(TeamSet.HabboColours)
            .Should()
            .BeFalse("the same colours in a different order are a different team space");
    }

    [Fact]
    public void PerTeamCapacity_IsPerTeam()
    {
        // The old layout had one capacity for the whole game. Teams can now differ, which is what an
        // asymmetric game (one seeker, five hiders) needs.
        TeamSet withCaps = TeamSet
            .Of("seeker", "hiders")
            .WithCapacity(new TeamId(1), 1)
            .WithCapacity(new TeamId(2), 5);

        withCaps.CapacityOf(new TeamId(1)).Should().Be(1);
        withCaps.CapacityOf(new TeamId(2)).Should().Be(5);

        TeamBook book = new(withCaps);
        book.JoinTeam(P(1), new TeamId(1));

        TeamGateRules
            .Toggle(book, withCaps, P(2), new TeamId(1), acceptingPlayers: true)
            .Should()
            .Be(TeamGateResult.None, "the seeker slot is taken");
        TeamGateRules
            .Toggle(book, withCaps, P(2), new TeamId(2), acceptingPlayers: true)
            .Should()
            .Be(TeamGateResult.Joined);
    }

    [Fact]
    public void ABookRefusesATeamItsSetDoesNotDeclare()
    {
        TeamBook book = new(TeamSet.Of("a", "b"));

        book.Knows(new TeamId(3)).Should().BeFalse();
        book.JoinTeam(P(1), new TeamId(3)).Should().BeFalse();

        book.AddScore(new TeamId(3), 10);
        book.GetTeamScore(new TeamId(3)).Should().Be(0);
    }
}
