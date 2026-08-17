using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The colour-from-suffix resolution that lets one logic class claim all four colour keys of a game
/// furni family. Both naming worlds must resolve: the logic keys spell colours out
/// (<c>freeze_gate_red</c>, <c>battlebanzai_gate_yellow</c>) while classnames abbreviate them
/// (<c>bb_score_r</c>, <c>fball_goal_g</c>). An unknown suffix must resolve to None — this runs in
/// logic constructors, where a throw would fail the item attach and take the furni out of the room.
/// </summary>
public sealed class GameColorKeyTests
{
    [Theory]
    [InlineData("freeze_gate_red", GameTeamColor.Red)]
    [InlineData("freeze_counter_green", GameTeamColor.Green)]
    [InlineData("battlebanzai_gate_blue", GameTeamColor.Blue)]
    [InlineData("battlebanzai_gate_yellow", GameTeamColor.Yellow)]
    [InlineData("bb_score_r", GameTeamColor.Red)]
    [InlineData("bb_gate_g", GameTeamColor.Green)]
    [InlineData("fball_goal_b", GameTeamColor.Blue)]
    [InlineData("fball_score_y", GameTeamColor.Yellow)]
    public void KnownSuffixes_ResolveToTheirColour(string key, GameTeamColor expected) =>
        GameColorKey.FromKeySuffix(key).Should().Be(expected);

    [Theory]
    [InlineData("freeze_tile")]
    [InlineData("battlebanzai_tile")]
    [InlineData("game_timer")]
    [InlineData("")]
    public void UnknownSuffixes_ResolveToNone_NeverThrow(string key) =>
        GameColorKey.FromKeySuffix(key).Should().Be(GameTeamColor.None);
}
