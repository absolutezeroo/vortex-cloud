using System.Collections.Immutable;
using FluentAssertions;
using Vortex.Primitives.Players.Avatar;
using Xunit;

namespace Vortex.Players.Tests.Avatar;

/// <summary>
/// Reading the set ids out of a look. This is what decides whether a saved figure is wearing
/// something the account has not unlocked, so the two ways it could be wrong are opposite and both
/// bad: miss a set and the check is decorative, invent one and a player cannot dress.
/// </summary>
public sealed class FigureStringTests
{
    [Fact]
    public void ASetIdIsTheSecondFieldOfEachPart()
    {
        FigureString
            .SetIdsOf("hd-180-1.ch-3216-66-1.lg-270-82")
            .Should()
            .BeEquivalentTo([180, 3216, 270]);
    }

    /// <summary>
    /// The same set can appear under two types in one look; ownership is asked once per set, so the
    /// list is distinct.
    /// </summary>
    [Fact]
    public void RepeatedSets_AreReportedOnce()
    {
        FigureString.SetIdsOf("hd-180-1.ch-180-66").Should().BeEquivalentTo([180]);
    }

    /// <summary>
    /// A part it cannot read names no set and can therefore grant nothing. Skipping it is safe;
    /// refusing the whole figure over it would block a look for a harmless oddity.
    /// </summary>
    [Theory]
    [InlineData("hd-180-1..ch-3216-66")]
    [InlineData("hd-180-1.broken.ch-3216-66")]
    [InlineData("hd--1.ch-3216-66.lg-180-82")]
    [InlineData("hd-abc-1.ch-3216-66.lg-180-82")]
    public void MalformedParts_AreSkippedAndTheRestIsStillRead(string figure)
    {
        ImmutableArray<int> setIds = FigureString.SetIdsOf(figure);

        setIds.Should().Contain(3216, "the well-formed parts still have to be read");
        setIds.Should().NotContain(0, "an unreadable part must not become set 0");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoFigure_IsNoSets(string? figure)
    {
        FigureString.SetIdsOf(figure).Should().BeEmpty();
    }

    /// <summary>
    /// A negative number is not a set id. `NumberStyles.None` refuses the sign, which matters because
    /// a set id parsed as -180 would match nothing and quietly pass a check it should never reach.
    /// </summary>
    [Fact]
    public void NegativeNumbers_AreNotSetIds()
    {
        FigureString.SetIdsOf("hd--180-1.ch-3216-66").Should().BeEquivalentTo([3216]);
    }
}
