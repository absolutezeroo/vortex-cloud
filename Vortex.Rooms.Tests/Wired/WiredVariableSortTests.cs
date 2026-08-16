using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The six ways the variable filters rank what they keep. The dropdown ids are what the client
/// sends, and the pairs are not laid out the way a reader expects — the "latest" of each pair is
/// the odd id, so an off-by-one here silently reverses every ranking.
/// </summary>
public sealed class WiredVariableSortTests
{
    [Theory]
    [InlineData(WiredVariableSort.HighestValue, true)]
    [InlineData(WiredVariableSort.LowestValue, true)]
    [InlineData(WiredVariableSort.OldestCreation, false)]
    [InlineData(WiredVariableSort.LatestUpdate, false)]
    public void ValueModesRankByTheValue(WiredVariableSort sort, bool expected) =>
        sort.RanksByValue().Should().Be(expected);

    [Theory]
    [InlineData(WiredVariableSort.OldestCreation, true)]
    [InlineData(WiredVariableSort.LatestCreation, true)]
    [InlineData(WiredVariableSort.OldestUpdate, false)]
    [InlineData(WiredVariableSort.LatestUpdate, false)]
    public void CreationModesRankByTheCreationMoment(WiredVariableSort sort, bool expected) =>
        sort.RanksByCreation().Should().Be(expected);

    [Theory]
    // The biggest number wins for "highest" and for both "latest" — a later moment is a bigger one.
    [InlineData(WiredVariableSort.HighestValue, true)]
    [InlineData(WiredVariableSort.LatestCreation, true)]
    [InlineData(WiredVariableSort.LatestUpdate, true)]
    [InlineData(WiredVariableSort.LowestValue, false)]
    [InlineData(WiredVariableSort.OldestCreation, false)]
    [InlineData(WiredVariableSort.OldestUpdate, false)]
    public void OnlyTheHighestAndLatestModesRankDownwards(WiredVariableSort sort, bool expected) =>
        sort.WantsDescending().Should().Be(expected);

    [Fact]
    public void TheDropdownIdsAreTheWireValues()
    {
        // Sent as the dropdown's own id, so the numbering is part of the protocol.
        ((int)WiredVariableSort.HighestValue)
            .Should()
            .Be(0);
        ((int)WiredVariableSort.LowestValue).Should().Be(1);
        ((int)WiredVariableSort.OldestCreation).Should().Be(2);
        ((int)WiredVariableSort.LatestCreation).Should().Be(3);
        ((int)WiredVariableSort.OldestUpdate).Should().Be(4);
        ((int)WiredVariableSort.LatestUpdate).Should().Be(5);
    }
}
