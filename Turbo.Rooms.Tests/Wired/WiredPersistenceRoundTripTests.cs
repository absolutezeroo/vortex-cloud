using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Turbo.Furniture;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Rooms.Wired;
using Xunit;

namespace Turbo.Rooms.Tests.Wired;

/// <summary>
/// Proves the wired persistence constat from DATA-MODEL §6: a configured wired box must keep
/// its configuration across a room unload / reload.
/// <para>
/// These tests exercise the real persistence boundary — <see cref="ExtraData"/> plus
/// <see cref="WiredData"/> — using the exact serialize/deserialize calls the runtime uses:
/// <c>FurnitureWiredLogic</c> writes its <see cref="WiredData"/> into the furni's
/// <c>extra_data</c> "wired" section (via <see cref="JsonSerializer.SerializeToNode(object?, System.Type, JsonSerializerOptions?)"/>
/// and <see cref="IExtraData.UpdateSection{TSection}"/>), the persistence grain stores
/// <see cref="ExtraData.GetJsonString"/> in the <c>extra_data</c> column, and on activation
/// <c>FillInternalDataAsync</c> reads it back with <c>Deserialize&lt;WiredData&gt;()</c>.
/// </para>
/// </summary>
public sealed class WiredPersistenceRoundTripTests
{
    /// <summary>
    /// Serializes a <see cref="WiredData"/> into a furni's <c>extra_data</c> exactly the way
    /// <c>FurnitureWiredLogic</c> does, then returns the persisted column string.
    /// </summary>
    private static string PersistWired(WiredData wired, string? existingExtraData = null)
    {
        var extraData = new ExtraData(existingExtraData);

        // Mirrors FurnitureWiredLogic.FillInternalDataAsync's SetAction callback.
        extraData.UpdateSection(
            ExtraDataSectionType.WIRED,
            JsonSerializer.SerializeToNode(wired, wired.GetType())
        );

        // Mirrors RoomItemSnapshot.ExtraData -> FurnitureEntity.ExtraData column.
        return extraData.GetJsonString();
    }

    /// <summary>
    /// Reads the "wired" section back out exactly the way <c>FillInternalDataAsync</c> does.
    /// </summary>
    private static WiredData? ReloadWired(string persistedExtraData)
    {
        // Mirrors RoomItemsProvider.LoadByRoomIdAsync rehydrating the column into ExtraData.
        var extraData = new ExtraData(persistedExtraData);

        return extraData.TryGetSection(ExtraDataSectionType.WIRED, out var wiredElement)
            ? wiredElement.Deserialize<WiredData>()
            : null;
    }

    [Fact]
    public void Configure_Unload_Reload_PreservesWiredConfig()
    {
        // Arrange — a player places a wired box and configures it (selected items, delays,
        // conditions, variables) just like ApplyWiredUpdateAsync would set on _wiredData.
        var configured = new WiredData
        {
            IntParams = new List<int> { 5, 1, 0, 250 },
            StringParam = "hello wired",
            StuffIds = new List<int> { 101, 102, 103 },
            StuffIds2 = new List<int> { 201, 202 },
            VariableIds = new List<string> { "var-a", "var-b" },
        };

        // Act — room runs (persist to extra_data), unloads (column string), reloads.
        var persisted = PersistWired(configured);
        var reloaded = ReloadWired(persisted);

        // Assert — the configuration is identical after the round-trip.
        reloaded.Should().NotBeNull();
        reloaded!.IntParams.Should().Equal(configured.IntParams);
        reloaded.StringParam.Should().Be(configured.StringParam);
        reloaded.StuffIds.Should().Equal(configured.StuffIds);
        reloaded.StuffIds2.Should().Equal(configured.StuffIds2);
        reloaded.VariableIds.Should().Equal(configured.VariableIds);
    }

    [Fact]
    public void Reload_From_FreshExtraData_YieldsDefaultConfig()
    {
        // A wired box that was never configured has no "wired" section; reload must not throw
        // and must surface "no persisted config" rather than corrupt data.
        var reloaded = ReloadWired("{}");

        reloaded.Should().BeNull();
    }

    [Fact]
    public void Persisting_Wired_DoesNotClobber_Other_Furni_Sections()
    {
        // DoD: "aucune autre furni n'est impactée". At the data level, writing the wired
        // section must preserve any sibling section (e.g. a furni's "stuff"/state section),
        // and vice versa — the sections are independent keys in the same extra_data blob.
        var withStuff = new ExtraData(null);
        withStuff.UpdateSection(ExtraDataSectionType.STUFF, JsonSerializer.SerializeToNode("5"));

        var wired = new WiredData
        {
            IntParams = new List<int> { 7 },
            StringParam = "keep-me",
        };
        var persisted = PersistWired(wired, withStuff.GetJsonString());

        // The stuff (state) section is still present and untouched...
        var roundTripped = new ExtraData(persisted);
        roundTripped
            .TryGetSection(ExtraDataSectionType.STUFF, out var stuffElement)
            .Should()
            .BeTrue();
        stuffElement.GetString().Should().Be("5");

        // ...and the wired config also round-trips alongside it.
        var reloaded = ReloadWired(persisted);
        reloaded.Should().NotBeNull();
        reloaded!.IntParams.Should().Equal(wired.IntParams);
        reloaded.StringParam.Should().Be(wired.StringParam);
    }
}
