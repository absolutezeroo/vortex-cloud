using System;
using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The two filters the calendar conditions are built from. Both have a "no restriction" state that
/// is easy to get backwards, and getting it backwards makes a freshly placed box either always true
/// or impossible to satisfy — neither of which looks like a bug to whoever placed it.
/// </summary>
public sealed class WiredChronoFilterTests
{
    [Fact]
    public void ARangeThatIsSkipped_PassesWhateverTheValue()
    {
        WiredChronoFilter.RangeMatches(use: false, value: 99, min: 1, max: 2).Should().BeTrue();
    }

    [Theory]
    [InlineData(5, 1, 10, true)]
    [InlineData(1, 1, 10, true)]
    [InlineData(10, 1, 10, true)]
    [InlineData(0, 1, 10, false)]
    [InlineData(11, 1, 10, false)]
    public void ARangeIsInclusiveAtBothEnds(int value, int min, int max, bool expected)
    {
        WiredChronoFilter.RangeMatches(use: true, value, min, max).Should().Be(expected);
    }

    [Fact]
    public void ARangeTypedBackwards_StillMeansBetweenTheTwo()
    {
        // The form has two independent spinners; nothing stops a player putting the larger first.
        WiredChronoFilter.RangeMatches(use: true, value: 5, min: 10, max: 1).Should().BeTrue();
    }

    [Fact]
    public void AnEmptyMask_MeansAny()
    {
        // Nothing ticked is the client's "no restriction"; reading it as "none" would make the box
        // impossible to satisfy, and the client lets a player save it that way.
        for (int weekday = 1; weekday <= 7; weekday++)
        {
            WiredChronoFilter.MaskMatches(0, weekday).Should().BeTrue();
        }
    }

    [Fact]
    public void MondayIsBitZero()
    {
        // The labels are numbered from one and the checkboxes indexed from zero. A hotel numbering
        // its week from Sunday would shift every box by a day.
        WiredChronoFilter.MaskMatches(0b000_0001, 1).Should().BeTrue();
        WiredChronoFilter.MaskMatches(0b000_0001, 2).Should().BeFalse();

        WiredChronoFilter.MaskMatches(0b100_0000, 7).Should().BeTrue();
        WiredChronoFilter.MaskMatches(0b100_0000, 1).Should().BeFalse();
    }

    [Fact]
    public void SeveralTickedValues_AllPass()
    {
        // Saturday and Sunday.
        int weekend = (1 << 5) | (1 << 6);

        WiredChronoFilter.MaskMatches(weekend, 6).Should().BeTrue();
        WiredChronoFilter.MaskMatches(weekend, 7).Should().BeTrue();
        WiredChronoFilter.MaskMatches(weekend, 3).Should().BeFalse();
    }

    [Fact]
    public void AValueOutsideTheMasksReach_DoesNotPass()
    {
        WiredChronoFilter.MaskMatches(0b1111, 0).Should().BeFalse();
        WiredChronoFilter.MaskMatches(0b1111, 33).Should().BeFalse();
    }

    [Fact]
    public void AnUnknownTimezone_FallsBackToUtc()
    {
        // A hotel moved to another host must not silently start matching different hours.
        WiredTimeZone.Resolve("Not/AZone").Should().Be(TimeZoneInfo.Utc);
        WiredTimeZone.Resolve(null).Should().Be(TimeZoneInfo.Utc);
        WiredTimeZone.Resolve("  ").Should().Be(TimeZoneInfo.Utc);
    }
}
