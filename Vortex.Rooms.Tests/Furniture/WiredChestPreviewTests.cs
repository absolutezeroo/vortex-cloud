using System;
using System.Collections.Generic;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// The chest preview is a string the client parses by splitting, so its shape is the contract and
/// nothing on either side throws when it is wrong — the icons simply never appear. These pin the
/// three details the client's furni-chest logic actually depends on
/// (<c>_SafeCls_1812.as::stringToItemType()</c>): the separators, the spelled-out boolean, and the
/// third field being absent rather than empty.
/// </summary>
public sealed class WiredChestPreviewTests
{
    private static IMapStuffData NewMap() =>
        new StuffDataFactory().CreateStuffData(StuffDataType.MapKey) as IMapStuffData
        ?? throw new InvalidOperationException("MapKey did not produce a map.");

    [Fact]
    public void Preview_joins_kinds_with_semicolons_and_fields_with_commas()
    {
        IMapStuffData map = NewMap();

        WiredChestStuffData.ApplyPreview(
            map,
            new List<ChestPreviewKind>
            {
                new(IsWallItem: false, SpriteId: 1234, Extra: string.Empty),
                new(IsWallItem: true, SpriteId: 55, Extra: "3"),
            }
        );

        map.Data[WiredChestStuffData.Visuals].Should().Be("false,1234;true,55,3");
    }

    [Fact]
    public void Preview_writes_an_empty_value_rather_than_dropping_the_key()
    {
        IMapStuffData map = NewMap();

        WiredChestStuffData.ApplyPreview(map, []);

        map.Data.Should().ContainKey(WiredChestStuffData.Visuals);
        map.Data[WiredChestStuffData.Visuals].Should().BeEmpty();
    }

    [Fact]
    public void Open_state_is_odd_and_closed_is_even()
    {
        IMapStuffData map = NewMap();

        WiredChestStuffData.ApplyState(map, open: true);

        (int.Parse(map.Data[WiredChestStuffData.State]) % 2).Should().Be(1);

        WiredChestStuffData.ApplyState(map, open: false);

        (int.Parse(map.Data[WiredChestStuffData.State]) % 2).Should().Be(0);
    }
}
