using System.Collections.Generic;
using FluentAssertions;
using Vortex.Specs.Naming;
using Xunit;

namespace Vortex.Specs.Tests.Reasoning;

public class HeaderTableFolderTests
{
    [Fact]
    public void Two_constants_reducing_to_one_name_are_reported_not_thrown_or_dropped()
    {
        // Arcturus really does declare both of these. A plain dictionary insert throws on the second
        // and a last-write-wins fold picks whichever the file system enumerated last.
        HeaderTableFolder.Result result = HeaderTableFolder.Fold(
            new Dictionary<string, int>
            {
                ["RoomEntryInfoMessageComposer"] = 749,
                ["RoomEntryInfoMessage"] = 21,
                ["MoveObjectMessageEvent"] = 248,
            }
        );

        result.Table.Should().ContainKey("RoomEntryInfo");
        result.Table["MoveObject"].Should().Be(248);
        result.Collisions.Should().ContainSingle();
        result.Collisions[0].Should().Contain("RoomEntryInfo");
    }

    [Fact]
    public void The_winner_of_a_collision_is_a_property_of_the_data_not_of_enumeration_order()
    {
        Dictionary<string, int> forwards = new()
        {
            ["RoomEntryInfoMessageComposer"] = 749,
            ["RoomEntryInfoMessage"] = 21,
        };
        Dictionary<string, int> backwards = new()
        {
            ["RoomEntryInfoMessage"] = 21,
            ["RoomEntryInfoMessageComposer"] = 749,
        };

        HeaderTableFolder
            .Fold(forwards)
            .Table["RoomEntryInfo"]
            .Should()
            .Be(HeaderTableFolder.Fold(backwards).Table["RoomEntryInfo"]);
    }

    [Fact]
    public void Two_constants_agreeing_on_the_id_is_not_a_collision()
    {
        HeaderTableFolder.Result result = HeaderTableFolder.Fold(
            new Dictionary<string, int> { ["PingMessageEvent"] = 10, ["PingEvent"] = 10 }
        );

        result.Table["Ping"].Should().Be(10);
        result.Collisions.Should().BeEmpty();
    }
}
