using System.Collections.Generic;
using FluentAssertions;
using Vortex.Specs.Completeness;
using Xunit;

namespace Vortex.Specs.Tests.Completeness;

/// <summary>
/// Reading the furniture logic surface off the asset-derived binding pass.
/// </summary>
/// <remarks>
/// The count is the thing that matters and the thing most easily got wrong: it comes from the
/// banner the generator writes above each statement, not from counting the classnames in the
/// <c>IN</c> list — a classname is not a key and repeats there, so counting entries would report a
/// smaller number than the statement actually touches and quietly rank the gaps in the wrong order.
/// </remarks>
public class FurnitureSurfaceTests
{
    private const string Pass = """
        -- 31967 definitions
        UPDATE `furniture_definitions`
           SET `logic` = 'furniture_multistate'
         WHERE `name` IN (
            '01_caterbody', '01_caterbody', '01_caterhead'
        );

        -- 2862 definitions
        UPDATE `furniture_definitions`
           SET `logic` = 'furniture_purchasable_clothing'
         WHERE `name` IN (
            'clothing_a'
        );
        """;

    [Fact]
    public void EachStatement_TakesTheCountFromItsBanner()
    {
        IReadOnlyDictionary<string, int> bindings = FurnitureSurfaceAnalyzer.ParseSeedBindings(
            Pass
        );

        bindings.Should().HaveCount(2);
        // Three classnames in the IN list, one of them repeated — the banner says 31967 and the
        // banner is right.
        bindings["furniture_multistate"].Should().Be(31967);
        bindings["furniture_purchasable_clothing"].Should().Be(2862);
    }

    /// <summary>
    /// SQL doubles a quote to escape it. Left as-is, <c>Chama D''agua</c> would never match the
    /// asset's own name and would look like a logic nobody uses.
    /// </summary>
    [Fact]
    public void AnEscapedQuote_ComesBackAsOne()
    {
        IReadOnlyDictionary<string, int> bindings = FurnitureSurfaceAnalyzer.ParseSeedBindings(
            """
            -- 2 definitions
            UPDATE `furniture_definitions`
               SET `logic` = 'Chama D''agua'
             WHERE `name` IN ('x');
            """
        );

        bindings.Should().ContainKey("Chama D'agua");
    }

    [Fact]
    public void OneLogicWrittenTwice_SumsItsDefinitions()
    {
        IReadOnlyDictionary<string, int> bindings = FurnitureSurfaceAnalyzer.ParseSeedBindings(
            """
            -- 10 definitions
            UPDATE `furniture_definitions` SET `logic` = 'furniture_basic' WHERE `name` IN ('a');

            -- 5 definitions
            UPDATE `furniture_definitions`
               SET `logic` = 'furniture_basic'
             WHERE `name` IN ('b');
            """
        );

        bindings["furniture_basic"].Should().Be(15);
    }

    /// <summary>
    /// A statement with no banner above it contributes no count. Inventing one — or borrowing the
    /// previous statement's — would put a number in the ranking that the file never claimed.
    /// </summary>
    [Fact]
    public void AnAssignmentWithNoBannerAboveIt_ContributesNothing()
    {
        FurnitureSurfaceAnalyzer
            .ParseSeedBindings("   SET `logic` = 'furniture_orphan'")
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void AClassRegisteringSeveralNames_YieldsAllOfThem()
    {
        IReadOnlyList<string> keys = FurnitureSurfaceAnalyzer.ParseRegisteredLogics(
            """
            [RoomObjectLogic("furniture_multistate")]
            [RoomObjectLogic("furniture_muItistate")]
            public class FurnitureFloorLogic { }
            """
        );

        // The second is the asset's own typo — a capital I for an l — registered deliberately so
        // the definitions carrying it still resolve.
        keys.Should().Equal("furniture_multistate", "furniture_muItistate");
    }

    [Fact]
    public void AClassRegisteringNothing_YieldsNothing()
    {
        FurnitureSurfaceAnalyzer
            .ParseRegisteredLogics("public class FurnitureLogic { }")
            .Should()
            .BeEmpty();
    }
}
