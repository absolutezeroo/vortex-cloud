using FluentAssertions;
using Vortex.Inventory.Fulfillment;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.Database.Tests.Catalog;

/// <summary>
/// A bot product describes itself in semicolon-separated key:value pairs —
/// <c>name:Robbie;figure:...;gender:m</c>. Reading the first segment as the figure, which is what a
/// bare-figure format would mean, put the literal text "name:Robbie" in the figure column and gave
/// the buyer a bot with no appearance at all. The keys are what make it unambiguous, and a figure
/// string contains neither a semicolon nor a colon, so the two formats cannot be confused.
/// </summary>
public sealed class BotProductParsingTests
{
    private const string Figure = "hd-180-1.ch-255-66.lg-270-82";

    [Fact]
    public void AKeyedProduct_TakesTheFigureFromItsFigureKeyNotItsFirstSegment()
    {
        BotCreateRequest? bot = BotProductReader.TryRead(
            $"name:Robbie;figure:{Figure};gender:m",
            string.Empty
        );

        bot.Should().NotBeNull();
        bot!.Figure.Should().Be(Figure, "the figure is the value of the figure key");
        bot.Name.Should().Be("Robbie");
        bot.Gender.Should().Be(AvatarGenderType.Male);
    }

    [Fact]
    public void TheKeysAreOrderIndependent()
    {
        BotCreateRequest? bot = BotProductReader.TryRead(
            $"gender:f;figure:{Figure};motto:at your service;name:Rosa",
            string.Empty
        );

        bot.Should().NotBeNull();
        bot!.Figure.Should().Be(Figure);
        bot.Name.Should().Be("Rosa");
        bot.Gender.Should().Be(AvatarGenderType.Female);
        bot.Motto.Should().Be("at your service");
    }

    [Fact]
    public void ABareFigureIsStillAccepted()
    {
        // What a hand-written product looks like. Rejecting it would be a trap, not a rule.
        BotCreateRequest? bot = BotProductReader.TryRead(Figure, "Custom");

        bot.Should().NotBeNull();
        bot!.Figure.Should().Be(Figure);
        bot.Name.Should().Be("Custom", "with no name key, the typed name stands");
    }

    [Fact]
    public void TheProductsNameWinsOverAnythingTheBuyerTyped()
    {
        // Habbo does not ask for a bot's name at the till the way it does for a pet; the product
        // names it. Letting the purchase field win would let a buyer rename a branded bot.
        BotCreateRequest? bot = BotProductReader.TryRead(
            $"name:Robbie;figure:{Figure}",
            "Something Else"
        );

        bot!.Name.Should().Be("Robbie");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name:Robbie;gender:m")]
    public void AProductWithNoFigure_IsRefusedRatherThanMintedInvisible(string? extraParam)
    {
        BotProductReader
            .TryRead(extraParam, "Robbie")
            .Should()
            .BeNull("a bot with no appearance is worse than a purchase that visibly failed");
    }

    [Fact]
    public void AnUnknownGender_FallsBackToMaleRatherThanThrowing()
    {
        BotCreateRequest? bot = BotProductReader.TryRead(
            $"figure:{Figure};gender:whatever",
            string.Empty
        );

        bot!.Gender.Should().Be(AvatarGenderType.Male);
    }
}
