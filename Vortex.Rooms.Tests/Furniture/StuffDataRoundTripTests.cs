using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// Stuff data is written into the extra-data blob by one component and read back by another, and the
/// two disagreed: the writer applies a camelCase naming policy (<c>{"data":"5"}</c>) while the
/// factory deserialized case-sensitively into <c>Data</c>, so every saved value came back as the
/// type's default. Nothing failed loudly — a dice face, a gate's open flag or a trophy's inscription
/// simply reset on the next load — which is exactly the kind of regression that needs a test rather
/// than a comment.
/// </summary>
public sealed class StuffDataRoundTripTests
{
    private static readonly StuffDataFactory Factory = new();

    private static string WriteStuffSection(object section)
    {
        ExtraData extraData = new(null);

        extraData.UpdateSection(ExtraDataSectionType.STUFF, section);

        return extraData.GetJsonString();
    }

    [Fact]
    public void LegacyStuffData_SurvivesAWriteThenRead()
    {
        string blob = WriteStuffSection(new { Data = "5" });

        IStuffData read = Factory.CreateStuffDataFromJson(StuffDataType.LegacyKey, blob);

        read.GetLegacyString().Should().Be("5");
    }

    [Fact]
    public void LegacyStuffData_SurvivesTheExtraDataOverload()
    {
        string blob = WriteStuffSection(new { Data = "owner\t29-07-2026\thello" });

        IStuffData read = Factory.CreateStuffDataFromExtraData(
            StuffDataType.LegacyKey,
            new ExtraData(blob)
        );

        read.GetLegacyString().Should().Be("owner\t29-07-2026\thello");
    }

    [Fact]
    public void UniqueMarkers_SurviveToo()
    {
        string blob = WriteStuffSection(
            new
            {
                Data = "1",
                UniqueNumber = 3,
                UniqueSeries = 100,
            }
        );

        IStuffData read = Factory.CreateStuffDataFromJson(StuffDataType.LegacyKey, blob);

        read.UniqueNumber.Should().Be(3);
        read.UniqueSeries.Should().Be(100);
        read.IsUnique().Should().BeTrue();
    }

    [Fact]
    public void AbsentSection_FallsBackToADefaultInstance()
    {
        IStuffData read = Factory.CreateStuffDataFromJson(StuffDataType.LegacyKey, "{}");

        read.GetLegacyString().Should().Be("0");
    }
}
