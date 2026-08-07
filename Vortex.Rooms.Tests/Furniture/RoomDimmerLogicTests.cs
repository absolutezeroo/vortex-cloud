using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Rooms.Object.Logic.Furniture.Wall;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// The moodlight's live setting is one comma-separated legacy string that the client splits into
/// five fields, and its three presets live somewhere else entirely. Both halves have a failure mode
/// that is invisible server-side: a state field written zero-based leaves every client showing an
/// unlit lamp, and presets that never reach extra data come back as factory colours after a room
/// reload.
/// </summary>
public sealed class RoomDimmerLogicTests
{
    [Fact]
    public void NewDimmer_IsOffWithThreePresetsFromThePalette()
    {
        FurnitureRoomDimmerLogic dimmer = Build(new ExtraData(null));

        dimmer.IsOn.Should().BeFalse();
        dimmer.SelectedPresetId.Should().Be(1);

        ImmutableArray<RoomDimmerPresetSnapshot> presets = dimmer.GetPresets();

        presets.Should().HaveCount(FurnitureRoomDimmerLogic.PresetCount);
        presets.Select(p => p.Id).Should().Equal(1, 2, 3);
        presets.Should().OnlyContain(p => p.ColorHex.Length == 7 && p.ColorHex[0] == '#');
    }

    [Fact]
    public async Task TogglePower_WritesTheOneBasedStateAndTheSelectedPresetsColour()
    {
        FurnitureRoomDimmerLogic dimmer = Build(new ExtraData(null));

        (await dimmer.TogglePowerAsync()).Should().BeTrue();

        string[] fields = dimmer.GetLegacyString().Split(',');

        // "2" is on, not "1": the client reads this field as parseInt(x) - 1.
        fields.Should().HaveCount(5);
        fields[0].Should().Be("2");
        fields[1].Should().Be("1");
        fields[3].Should().Be(dimmer.GetPresets()[0].ColorHex);
        dimmer.IsOn.Should().BeTrue();

        (await dimmer.TogglePowerAsync()).Should().BeFalse();

        dimmer.GetLegacyString().Split(',')[0].Should().Be("1");
        dimmer.IsOn.Should().BeFalse();
    }

    [Fact]
    public async Task SavePreset_WithApply_SwitchesTheRoomToItAndSurvivesAReload()
    {
        IExtraData extraData = new ExtraData(null);
        FurnitureRoomDimmerLogic dimmer = Build(extraData);

        await dimmer.SavePresetAsync(
            2,
            effectId: 2,
            colorHex: "#123456",
            brightness: 200,
            apply: true
        );

        string[] fields = dimmer.GetLegacyString().Split(',');

        fields[0].Should().Be("2");
        fields[1].Should().Be("2");
        fields[2].Should().Be("2");
        fields[3].Should().Be("#123456");
        fields[4].Should().Be("200");

        FurnitureRoomDimmerLogic reloaded = Build(new ExtraData(extraData.GetJsonString()));

        reloaded.GetPresets()[1].ColorHex.Should().Be("#123456");
        reloaded.GetPresets()[1].Brightness.Should().Be(200);
        reloaded.GetPresets()[1].EffectId.Should().Be(2);
    }

    [Fact]
    public async Task SavePreset_WithoutApply_LeavesAnUnrelatedSelectionAlone()
    {
        FurnitureRoomDimmerLogic dimmer = Build(new ExtraData(null));

        await dimmer.TogglePowerAsync();
        await dimmer.SavePresetAsync(
            3,
            effectId: 1,
            colorHex: "#ABCDEF",
            brightness: 120,
            apply: false
        );

        // Preset 1 is showing; storing preset 3 must not repaint the room.
        dimmer.GetLegacyString().Split(',')[1].Should().Be("1");
        dimmer.GetPresets()[2].ColorHex.Should().Be("#ABCDEF");
    }

    [Fact]
    public async Task SavePreset_OverwritingTheLivePreset_RepaintsWithoutBeingAsked()
    {
        FurnitureRoomDimmerLogic dimmer = Build(new ExtraData(null));

        await dimmer.TogglePowerAsync();
        await dimmer.SavePresetAsync(
            1,
            effectId: 1,
            colorHex: "#0053F7",
            brightness: 90,
            apply: false
        );

        // Same slot the lamp is currently showing: leaving stuff data untouched would keep the old
        // colour in the room until someone toggled the switch.
        dimmer.GetLegacyString().Split(',')[3].Should().Be("#0053F7");
        dimmer.GetLegacyString().Split(',')[4].Should().Be("90");
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public async Task SavePreset_RejectsAColourTheClientCannotParse(string colorHex)
    {
        FurnitureRoomDimmerLogic dimmer = Build(new ExtraData(null));

        await dimmer.SavePresetAsync(
            1,
            effectId: 1,
            colorHex: colorHex,
            brightness: 255,
            apply: true
        );

        // parseInt(substr(1), 16) on any of these is NaN, which paints the room black.
        string stored = dimmer.GetPresets()[0].ColorHex;

        stored.Should().HaveLength(7);
        stored[0].Should().Be('#');
        dimmer.GetLegacyString().Split(',')[3].Should().Be(stored);
    }

    [Fact]
    public async Task SavePreset_ClampsBrightnessToWhatTheSliderCanReach()
    {
        FurnitureRoomDimmerLogic dimmer = Build(new ExtraData(null));

        await dimmer.SavePresetAsync(
            1,
            effectId: 1,
            colorHex: "#74F5F5",
            brightness: 0,
            apply: true
        );

        // Below the widget's own minimum the room goes dark and the dialog offers no way back up.
        dimmer.GetPresets()[0].Brightness.Should().Be(76);

        await dimmer.SavePresetAsync(
            1,
            effectId: 1,
            colorHex: "#74F5F5",
            brightness: 9000,
            apply: true
        );

        dimmer.GetPresets()[0].Brightness.Should().Be(255);
    }

    [Fact]
    public async Task SavePreset_IgnoresASlotTheDialogCannotShow()
    {
        FurnitureRoomDimmerLogic dimmer = Build(new ExtraData(null));

        await dimmer.SavePresetAsync(
            4,
            effectId: 1,
            colorHex: "#000000",
            brightness: 255,
            apply: true
        );

        dimmer.GetPresets().Should().HaveCount(FurnitureRoomDimmerLogic.PresetCount);
        dimmer.IsOn.Should().BeFalse();
    }

    private static FurnitureRoomDimmerLogic Build(IExtraData extraData) =>
        new(new StuffDataFactory(), StubContext(extraData));

    private static IRoomWallItemContext StubContext(IExtraData extraData)
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 40270,
            SpriteId = 4027,
            Name = "roomdimmer",
            ProductType = ProductType.Wall,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "furniture_roomdimmer",
            TotalStates = 1,
            Width = 1,
            Length = 1,
            StackHeight = default,
            CanStack = false,
            CanWalk = false,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = true,
            CanGroup = false,
            CanSell = true,
            UsagePolicy = FurnitureUsageType.Everybody,
            ExtraData = null,
            StuffDataType = StuffDataType.LegacyKey,
        };

        IRoomWallItem item = FakeProxy.Create<IRoomWallItem>(call =>
            call.Method.Name == "get_ExtraData" ? extraData : null
        );

        return FakeProxy.Create<IRoomWallItemContext>(call =>
            call.Method.Name switch
            {
                "get_Definition" => definition,
                "get_RoomObject" => item,
                _ => null,
            }
        );
    }
}
