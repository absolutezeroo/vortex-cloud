using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// A clothing-change booth holds two outfits in one string and the client is only ever told about
/// one of them at a time.
/// </summary>
/// <remarks>
/// The failure this guards against is silent and one-sided: set the girls' outfit and the boys' one
/// disappears, which nobody notices until a boy walks into the booth and is offered nothing. The
/// client reads index 0 as the boys' look and index 1 as the girls'
/// (<c>FurnitureClothingChangeLogic.updateClothingData</c>).
/// </remarks>
public sealed class ClothingChangeDataTests
{
    private const string Boy = "hd-180-1.ch-210-66";
    private const string Girl = "hd-600-1.ch-665-92";

    [Fact]
    public void SettingOneGender_KeepsTheOther()
    {
        string merged = ClothingChangeData.Merge($"{Boy},{Girl}", "F", "hd-605-2");

        merged.Should().Be($"{Boy},hd-605-2");
    }

    [Fact]
    public void SettingTheBoys_KeepsTheGirls()
    {
        string merged = ClothingChangeData.Merge($"{Boy},{Girl}", "M", "hd-185-3");

        merged.Should().Be($"hd-185-3,{Girl}");
    }

    [Fact]
    public void AnEmptyBooth_StillProducesBothHalves()
    {
        // The separator has to be there even when only one side is filled, or the client's split
        // returns a single element and the other gender reads as absent rather than as unset.
        ClothingChangeData.Merge(string.Empty, "M", Boy).Should().Be($"{Boy},");
        ClothingChangeData.Merge(string.Empty, "F", Girl).Should().Be($",{Girl}");
    }

    [Fact]
    public void AGenderTheClientNeverSends_WritesTheBoysSideRatherThanNothing()
    {
        // "M" and "F" are what the client sends. Anything else is not a reason to accept the packet
        // and change nothing, which would look exactly like a booth that refuses to save.
        ClothingChangeData
            .Merge($"{Boy},{Girl}", "?", "hd-999-9")
            .Should()
            .Be($"hd-999-9,{Girl}");
    }

    [Fact]
    public void DataThatWasNeverATwoPartString_IsTreatedAsTheBoysHalf()
    {
        // A booth carrying a single look from some earlier write, or from a hand-edited row.
        ClothingChangeData.Merge(Boy, "F", Girl).Should().Be($"{Boy},{Girl}");
    }

    [Fact]
    public void EachGender_ReadsItsOwnSide()
    {
        string booth = $"{Boy},{Girl}";

        ClothingChangeData.LookFor(booth, AvatarGenderType.Male).Should().Be(Boy);
        ClothingChangeData.LookFor(booth, AvatarGenderType.Female).Should().Be(Girl);
    }

    [Fact]
    public void AGenderWithNothingOnItsSide_ReadsEmpty()
    {
        // What tells the booth to dress nobody. Reading the boys' look for a girl would put every
        // girl in the boys' kit, which is worse than the booth doing nothing.
        ClothingChangeData.LookFor($"{Boy},", AvatarGenderType.Female).Should().BeEmpty();
        ClothingChangeData.LookFor(string.Empty, AvatarGenderType.Male).Should().BeEmpty();
    }

    [Fact]
    public void Dressing_KeepsTheWearersHead_AndTakesTheBoothsClothes()
    {
        const string Wearer = "hd-180-1.hr-828-45.ch-999-1.lg-999-1.sh-999-1";
        const string Kit = "hd-3-3.ch-210-66.lg-270-82.sh-305-62";

        // The head parts are the wearer's and the outfit is the booth's -- the booth's own hd is
        // dropped, which is what stops a kit turning everyone into the same person.
        ClothingChangeData
            .Dress(Wearer, Kit)
            .Should()
            .Be("hd-180-1.hr-828-45.ch-210-66.lg-270-82.sh-305-62");
    }

    [Fact]
    public void APartialKit_LeavesTheMissingPiecesOff()
    {
        // A booth holding only a shirt: the wearer loses their own legs rather than keeping them,
        // because the booth owns every clothing slot it is asked about. Pinned because the opposite
        // reading -- "merge, keeping what the kit does not mention" -- is the intuitive one and is
        // not what the reference emulator does.
        ClothingChangeData
            .Dress("hd-180-1.ch-999-1.lg-999-1", "ch-210-66")
            .Should()
            .Be("hd-180-1.ch-210-66");
    }
}
