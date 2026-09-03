using FluentAssertions;
using Vortex.Rooms.Games.Arena;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The structured arena check that replaced scattered boolean guards. The property that matters is
/// that a refusal SAYS WHAT IS MISSING: a match that silently did nothing was the old failure, and
/// it was indistinguishable from a match that ran badly.
/// </summary>
public sealed class ArenaValidationTests
{
    [Fact]
    public void AnEmptyValidation_CanStart()
    {
        ArenaValidation.Valid.CanStart.Should().BeTrue();
        ArenaValidation.Valid.DescribeShortfall().Should().BeEmpty();
    }

    [Fact]
    public void AMetRequirement_CanStart()
    {
        ArenaValidation validation = ArenaValidation
            .Builder()
            .Require("Banzai tiles", found: 74)
            .Build();

        validation.CanStart.Should().BeTrue();
        validation.DescribeShortfall().Should().BeEmpty();
    }

    [Fact]
    public void AnUnmetRequirement_BlocksAndNamesItself()
    {
        ArenaValidation validation = ArenaValidation
            .Builder()
            .Require("Football", found: 1)
            .Require("Goals of different colours", found: 1, required: 2)
            .Build();

        validation.CanStart.Should().BeFalse();
        validation.DescribeShortfall().Should().Be("Goals of different colours: 1/2");
    }

    [Fact]
    public void AnUnmetPreference_IsReportedButDoesNotBlock()
    {
        ArenaValidation validation = ArenaValidation
            .Builder()
            .Require("Freeze tiles", found: 20)
            .Prefer("Team gates", found: 0, required: 2)
            .Build();

        validation
            .CanStart.Should()
            .BeTrue("a Freeze arena with no gates is playable, it is just quieter");
        validation.DescribeShortfall().Should().Be("Team gates: 0/2");
    }

    [Fact]
    public void SeveralShortfalls_AreAllListed()
    {
        ArenaValidation validation = ArenaValidation
            .Builder()
            .Require("Football", found: 0)
            .Prefer("Team gates", found: 1, required: 2)
            .Build();

        validation.DescribeShortfall().Should().Be("Football: 0/1; Team gates: 1/2");
    }
}
