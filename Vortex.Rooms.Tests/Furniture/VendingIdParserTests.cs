using FluentAssertions;
using Vortex.Primitives.Furniture;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// Reading the hand-item list an operator typed into a vending machine.
/// </summary>
/// <remarks>
/// A trust boundary, not a formality: the column is free text filled in by hand through the
/// furniture admin page, and a value that throws here would take the whole furniture cache down with
/// it — every definition in the hotel is built in one pass. So the rule is that anything unusable is
/// dropped and the rest still loads.
/// </remarks>
public sealed class VendingIdParserTests
{
    [Theory]
    [InlineData("1,2,3")]
    [InlineData("1;2;3")]
    [InlineData("1 2 3")]
    [InlineData(" 1 , 2 ,3 ")]
    [InlineData("1,,2,3,")]
    public void TheSeparatorsAnOperatorPlausiblyTypesAllWork(string raw)
    {
        VendingIdParser.Parse(raw).Should().Equal([1, 2, 3]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NothingConfiguredIsAnEmptyList(string? raw)
    {
        VendingIdParser.Parse(raw).Should().BeEmpty();
    }

    /// <summary>
    /// Junk is dropped, not thrown on. A definition that refuses to load takes the furniture cache
    /// with it, and one mistyped machine must not cost the hotel its catalogue.
    /// </summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("99999999999999999999")]
    public void UnusableEntriesAreDroppedRatherThanThrown(string raw)
    {
        VendingIdParser.Parse(raw).Should().BeEmpty();
    }

    [Fact]
    public void TheUsableEntriesSurviveTheUnusableOnes()
    {
        VendingIdParser.Parse("7, oops, 0, 9").Should().Equal([7, 9]);
    }

    /// <summary>
    /// Duplicates are kept. Repeating an id is the only way a flat list can say "mostly water,
    /// sometimes champagne", and de-duplicating would silently flatten that back out.
    /// </summary>
    [Fact]
    public void ARepeatedIdIsKeptBecauseItIsHowAnOperatorWeightsIt()
    {
        VendingIdParser.Parse("1,1,1,2").Should().Equal([1, 1, 1, 2]);
    }
}
