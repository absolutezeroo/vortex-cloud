using System;
using FluentAssertions;
using Vortex.Primitives.MysteryBox;
using Xunit;

namespace Vortex.Players.Tests.MysteryBox;

/// <summary>
/// The client splits a trophy's stuff data on tabs into owner / date / message, so a tab inside any
/// field silently shifts every field after it — an inscription reading "a\tb" would push the real
/// message into the date slot.
/// </summary>
public sealed class MysteryTrophyStuffDataTests
{
    private static readonly DateOnly Date = new(2026, 7, 29);

    [Fact]
    public void BuildsTheThreeTabSeparatedFields()
    {
        MysteryTrophyStuffData
            .Build("Ctuto", Date, "Well played")
            .Should()
            .Be("Ctuto\t29-07-2026\tWell played");
    }

    [Fact]
    public void TabsInsideFields_CannotShiftTheLayout()
    {
        string built = MysteryTrophyStuffData.Build("Ct\tuto", Date, "a\tb");

        built.Split('\t').Should().HaveCount(3);
        built.Should().Be("Ct uto\t29-07-2026\ta b");
    }

    [Fact]
    public void NewlinesAreFlattened()
    {
        MysteryTrophyStuffData
            .Build("Ctuto", Date, "line one\r\nline two")
            .Should()
            .Be("Ctuto\t29-07-2026\tline one  line two");
    }

    [Fact]
    public void EmptyInscription_StillLeavesBothSeparators()
    {
        MysteryTrophyStuffData.Build("Ctuto", Date, "").Should().Be("Ctuto\t29-07-2026\t");
    }
}
