using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Vortex.Database.Seeds;
using Xunit;

namespace Vortex.Database.Tests.Pets;

/// <summary>
/// The pet food seed is the whole reason a pet ever eats: feeding refuses outright unless a row
/// exists for (this furni, this pet type). Nothing in the build or the room code can tell that a row
/// names the wrong species -- the pet simply walks to the bowl and stands there -- so the mapping is
/// pinned here.
/// </summary>
/// <remarks>
/// The seed it replaced assigned foods sequentially on a legend that did not match the client's
/// <c>pet.type.*</c> keys, which fed Salmon to spiders and Hay to the Monster, and stopped at type
/// 18 so no bunny, pigeon, baby, dinosaur or cow could eat at all.
/// </remarks>
public sealed class PetFoodSeedTests
{
    private const int Monsterplant = 16;
    private const int Lion = 6;
    private const int Spider = 8;
    private const int Chicken = 10;

    private const int ChocolateGazelle = 3599;
    private const int WebbedGrapes = 3816;
    private const int Corn = 4457;
    private const int RedWaterBowl = 1538;
    private const int BasicPinkWaterBowl = 4181;

    /// <summary>Every pet type the catalogue sells, from the <c>a0 pet&lt;N&gt;</c> products on its
    /// pets pages. Each of them has an owner who can put food down.</summary>
    private static readonly int[] SellablePetTypes =
    [
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8,
        9,
        10,
        11,
        12,
        14,
        15,
        17,
        18,
        19,
        20,
        21,
        22,
        23,
        24,
        25,
        28,
        29,
        30,
        31,
        32,
        35,
    ];

    private sealed record Row(
        int DefinitionId,
        int PetType,
        int Nutrition,
        int Energy,
        int MaxUses
    );

    private static readonly IReadOnlyList<Row> Rows = Parse();

    private static IReadOnlyList<Row> Parse()
    {
        MatchCollection matches = Regex.Matches(
            SeedScripts.Read("pet_food.sql"),
            @"^\s*\((\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+), NOW\(\), NOW\(\)\)",
            RegexOptions.Multiline
        );

        return
        [
            .. matches.Select(m => new Row(
                int.Parse(m.Groups[1].Value),
                int.Parse(m.Groups[2].Value),
                int.Parse(m.Groups[3].Value),
                int.Parse(m.Groups[4].Value),
                int.Parse(m.Groups[5].Value)
            )),
        ];
    }

    [Fact]
    public void TheSeedParses()
    {
        Rows.Should().NotBeEmpty("the regex has to keep matching the seed's row shape");
    }

    [Theory]
    [MemberData(nameof(SellableTypes))]
    public void EverySellablePetCanEatAndDrink(int petType)
    {
        Rows.Where(r => r.PetType == petType && r.Nutrition > 0)
            .Should()
            .NotBeEmpty($"pet type {petType} is sold, so something has to feed it");

        Rows.Where(r => r.PetType == petType && r.Energy > 0)
            .Should()
            .NotBeEmpty($"pet type {petType} is sold, so something has to water it");
    }

    public static TheoryData<int> SellableTypes()
    {
        TheoryData<int> data = [];

        foreach (int petType in SellablePetTypes)
        {
            data.Add(petType);
        }

        return data;
    }

    [Fact]
    public void TheMonsterplantIsNeverFed()
    {
        Rows.Where(r => r.PetType == Monsterplant)
            .Should()
            .BeEmpty(
                "monsterplants are rooted and RoomPetSystem skips them before feeding -- they are watered, not fed"
            );
    }

    /// <summary>
    /// Two foods name their species in their own furnidata description, so they are the assignments
    /// that cannot be a matter of taste.
    /// </summary>
    [Theory]
    [InlineData(ChocolateGazelle, Lion, "Sweets for your lion")]
    [InlineData(WebbedGrapes, Spider, "Juicy and delicious for spiders only")]
    public void TheFoodsThatNameTheirSpeciesFeedThatSpeciesAlone(
        int definitionId,
        int petType,
        string description
    )
    {
        Rows.Where(r => r.DefinitionId == definitionId)
            .Select(r => r.PetType)
            .Should()
            .Equal([petType], $"furnidata says \"{description}\"");
    }

    [Fact]
    public void CornFeedsTheChicken()
    {
        Rows.Where(r => r.DefinitionId == Corn)
            .Select(r => r.PetType)
            .Should()
            .Contain(Chicken, "furnidata says \"Your chicks will love it\"");
    }

    [Fact]
    public void WaterIsSpeciesBlind()
    {
        int[] drinkers =
        [
            .. Rows.Where(r => r.DefinitionId == RedWaterBowl).Select(r => r.PetType),
        ];

        drinkers.Should().Contain(SellablePetTypes, "any pet drinks from any bowl");
        drinkers.Should().NotContain(Monsterplant);
    }

    /// <summary>
    /// The one-state bowl families cannot show a fill level, so their servings come from here rather
    /// than from a state counting down. A missing row left fifteen bowls inert.
    /// </summary>
    [Fact]
    public void TheOneStateBowlsCarryTheirOwnServings()
    {
        Rows.Where(r => r.DefinitionId == BasicPinkWaterBowl)
            .Should()
            .NotBeEmpty()
            .And.OnlyContain(r => r.MaxUses > 0);
    }

    [Fact]
    public void EveryRowRestoresExactlyOneNeed()
    {
        Rows.Should()
            .OnlyContain(
                r => (r.Nutrition > 0) ^ (r.Energy > 0),
                "feeding refuses a row that restores neither, and a bowl is either food or drink"
            );
    }

    [Fact]
    public void EveryRowHasServings()
    {
        Rows.Should().OnlyContain(r => r.MaxUses > 0, "a bowl with no servings is binned on sight");
    }

    [Fact]
    public void NoDuplicateRowsPerFurniAndPetType()
    {
        Rows.GroupBy(r => (r.DefinitionId, r.PetType))
            .Where(g => g.Count() > 1)
            .Should()
            .BeEmpty("feeding reads the row with SingleOrDefaultAsync and throws on a duplicate");
    }
}
