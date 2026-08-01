using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Xunit;

namespace Vortex.Players.Tests;

public class NameChangePolicyTests
{
    private const int MinLength = 3;
    private const int MaxLength = 15;

    [Theory]
    [InlineData("Habbo")]
    [InlineData("user_1")]
    [InlineData("a-b.c")]
    [InlineData("123")]
    public void Validate_WellFormedName_ReturnsOk(string name) =>
        NameChangePolicy.Validate(name, MinLength, MaxLength).Should().Be(NameChangeResultCode.Ok);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_ReturnsNameRequired(string? name) =>
        NameChangePolicy
            .Validate(name, MinLength, MaxLength)
            .Should()
            .Be(NameChangeResultCode.NameRequired);

    [Fact]
    public void Validate_ShorterThanMinimum_ReturnsTooShort() =>
        NameChangePolicy
            .Validate("ab", MinLength, MaxLength)
            .Should()
            .Be(NameChangeResultCode.NameTooShort);

    [Fact]
    public void Validate_LongerThanMaximum_ReturnsTooLong() =>
        NameChangePolicy
            .Validate(new string('a', MaxLength + 1), MinLength, MaxLength)
            .Should()
            .Be(NameChangeResultCode.NameTooLong);

    [Theory]
    [InlineData("bad name")]
    [InlineData("bad!")]
    [InlineData("bad@name")]
    public void Validate_DisallowedCharacter_ReturnsNotValid(string name) =>
        NameChangePolicy
            .Validate(name, MinLength, MaxLength)
            .Should()
            .Be(NameChangeResultCode.NameNotValid);

    [Fact]
    public void BuildSuggestions_KeepsEverySuggestionWithinMaxLength()
    {
        string[] suggestions = NameChangePolicy.BuildSuggestions(
            new string('a', MaxLength),
            MaxLength,
            count: 3
        );

        suggestions.Should().HaveCount(3);
        suggestions.Should().OnlyContain(s => s.Length <= MaxLength);
        suggestions.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void BuildSuggestions_SuggestionsAreThemselvesValid()
    {
        string[] suggestions = NameChangePolicy.BuildSuggestions("Habbo", MaxLength, count: 3);

        suggestions
            .Should()
            .OnlyContain(s =>
                NameChangePolicy.Validate(s, MinLength, MaxLength) == NameChangeResultCode.Ok
            );
    }

    [Fact]
    public void BuildSuggestions_NoName_ReturnsEmpty() =>
        NameChangePolicy.BuildSuggestions(string.Empty, MaxLength, count: 3).Should().BeEmpty();
}
