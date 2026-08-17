using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Grains.Systems.Freeze;
using Xunit;

namespace Vortex.Rooms.Tests.Freeze;

/// <summary>
/// Locks the contract that makes the wired team leaves work during a Freeze round: the Freeze game keeps
/// its teams and scores in the room's shared <see cref="GameTeamState"/> rather than in a store of its
/// own. Everything the room owner can wire — <c>wf_cnd_actor_in_team</c>, <c>wf_slc_users_team</c>,
/// <c>wf_cnd_team_has_score</c>, <c>wf_cnd_team_has_rank</c> and the SCORE_ACHIEVED trigger — reads that
/// one state, so a Freeze player who is not in it is invisible to all of them.
/// </summary>
public sealed class FreezeSharedTeamStateTests
{
    private static PlayerId P(int id) => (PlayerId)id;

    private static (RoomFreezeGame Game, GameTeamState Teams) NewGame()
    {
        GameTeamState teams = new();

        return (new RoomFreezeGame { Teams = teams }, teams);
    }

    [Fact]
    public void Gate_Join_Registers_The_Player_In_The_Shared_Team_State()
    {
        (RoomFreezeGame game, GameTeamState teams) = NewGame();

        game.ToggleGate(P(1), GameTeamColor.Red).Should().Be(FreezeGateResult.Joined);

        // What wf_cnd_actor_in_team and wf_slc_users_team read.
        teams.GetTeam(P(1)).Should().Be(GameTeamColor.Red);
        teams.GetPlayersInTeam(GameTeamColor.Red).Should().ContainSingle().Which.Should().Be(P(1));
    }

    [Fact]
    public void Gate_Leave_Clears_The_Shared_Membership()
    {
        (RoomFreezeGame game, GameTeamState teams) = NewGame();
        game.ToggleGate(P(1), GameTeamColor.Red);

        game.ToggleGate(P(1), GameTeamColor.Red).Should().Be(FreezeGateResult.Left);

        teams.GetTeam(P(1)).Should().Be(GameTeamColor.None);
        teams.GetTeamMemberCount(GameTeamColor.Red).Should().Be(0);
    }

    [Fact]
    public void Gate_Switch_Moves_The_Shared_Membership()
    {
        (RoomFreezeGame game, GameTeamState teams) = NewGame();
        game.ToggleGate(P(1), GameTeamColor.Red);

        game.ToggleGate(P(1), GameTeamColor.Blue).Should().Be(FreezeGateResult.Joined);

        teams.GetTeam(P(1)).Should().Be(GameTeamColor.Blue);
        teams.GetTeamMemberCount(GameTeamColor.Red).Should().Be(0);
        teams.GetTeamMemberCount(GameTeamColor.Blue).Should().Be(1);
    }

    [Fact]
    public void Rejected_Switch_Leaves_The_Shared_Membership_Untouched()
    {
        GameTeamState teams = new();
        RoomFreezeGame game = new()
        {
            Teams = teams,
            Settings = FreezeSettings.Default with { MaxPlayersPerTeam = 1 },
        };
        game.ToggleGate(P(1), GameTeamColor.Blue); // Blue is now full
        game.ToggleGate(P(2), GameTeamColor.Red);

        game.ToggleGate(P(2), GameTeamColor.Blue).Should().Be(FreezeGateResult.None);

        // The rejected switch must not strip the shared membership either.
        teams.GetTeam(P(2)).Should().Be(GameTeamColor.Red);
    }

    [Fact]
    public void Leaving_The_Game_Removes_The_Player_From_The_Shared_Team()
    {
        (RoomFreezeGame game, GameTeamState teams) = NewGame();
        game.ToggleGate(P(1), GameTeamColor.Green);
        game.Start();

        // Elimination, the exit tile and leaving the room all funnel through Remove.
        game.Remove(P(1)).Should().NotBeNull();

        teams.GetTeam(P(1)).Should().Be(GameTeamColor.None);
    }

    [Fact]
    public void Start_Keeps_The_Teams_The_Gates_Picked()
    {
        (RoomFreezeGame game, GameTeamState teams) = NewGame();
        game.ToggleGate(P(1), GameTeamColor.Red);
        game.ToggleGate(P(2), GameTeamColor.Blue);

        game.Start().Should().BeTrue();

        // A GameTeamState.Reset() at kick-off would empty the arena: the gates pick the teams BEFORE the
        // round starts.
        teams.GetTeam(P(1)).Should().Be(GameTeamColor.Red);
        teams.GetTeam(P(2)).Should().Be(GameTeamColor.Blue);
        game.GetTeamCount(GameTeamColor.Red).Should().Be(1);
    }

    [Fact]
    public void Start_Does_Not_Clear_Scores_A_GameStarts_Box_Just_Awarded()
    {
        (RoomFreezeGame game, GameTeamState teams) = NewGame();
        game.ToggleGate(P(1), GameTeamColor.Red);

        // RoomGameSystem.StartGameAsync clears the scores and only then publishes GAME_STARTS, so a
        // wired give-score box reacting to that event has already run by the time Freeze starts. If
        // Freeze cleared the scores too, those points would vanish with nothing to show for it.
        teams.AddScore(GameTeamColor.Red, 15);

        game.Start().Should().BeTrue();

        game.GetTeamScore(GameTeamColor.Red).Should().Be(15);
    }

    [Fact]
    public void Freeze_Score_Is_The_Number_The_Wired_Conditions_Read()
    {
        (RoomFreezeGame game, GameTeamState teams) = NewGame();
        game.ToggleGate(P(1), GameTeamColor.Yellow);
        game.Start();

        teams.AddScore(GameTeamColor.Yellow, 7);

        game.GetTeamScore(GameTeamColor.Yellow).Should().Be(7);
        teams.GetTeamScore(GameTeamColor.Yellow).Should().Be(7);
    }

    [Fact]
    public void Winner_Is_Read_From_The_Shared_Scores()
    {
        (RoomFreezeGame game, GameTeamState teams) = NewGame();
        game.ToggleGate(P(1), GameTeamColor.Red);
        game.ToggleGate(P(2), GameTeamColor.Green);
        game.Start();

        teams.AddScore(GameTeamColor.Red, 10);
        teams.AddScore(GameTeamColor.Green, 25);

        game.Stop().Should().Be(GameTeamColor.Green);
    }

    [Fact]
    public void A_Scoreless_Round_Has_No_Winner()
    {
        (RoomFreezeGame game, _) = NewGame();
        game.ToggleGate(P(1), GameTeamColor.Red);
        game.Start();

        game.Stop().Should().Be(GameTeamColor.None);
    }
}
