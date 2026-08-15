using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Storage;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// A variable box's values live in its furni's extra data, and now so do the moments they were
/// written at. Both have to survive a room unload the same way, through the exact
/// serialize/deserialize the runtime uses — and a box saved before the room kept times has to keep
/// loading.
/// </summary>
public sealed class WiredVariableStorePersistenceTests
{
    private static readonly WiredVariableKey Key = new(
        WiredVariableId.Parse("4242"),
        WiredVariableTargetType.User,
        7
    );

    [Fact]
    public async Task ValuesAndTheirWriteTimes_SurviveAReload()
    {
        KeyValueStore store = new();

        await store.GiveValueAsync(Key, new WiredVariableValue(5));

        KeyValueStore reloaded = RoundTrip(store);

        reloaded.TryGetValue(Key, out WiredVariableValue value).Should().BeTrue();
        value.Value.Should().Be(5);

        reloaded
            .TryGetTimestamps(Key, out long createdAtMs, out long updatedAtMs)
            .Should()
            .BeTrue();

        createdAtMs.Should().BeCloseTo(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 60_000);
        updatedAtMs.Should().Be(createdAtMs);
    }

    [Fact]
    public async Task RewritingAValue_KeepsItsCreationTime()
    {
        KeyValueStore store = new();

        await store.GiveValueAsync(Key, new WiredVariableValue(5));

        store.TryGetTimestamps(Key, out long createdAtMs, out _).Should().BeTrue();

        await store.SetValueAsync(null!, Key, new WiredVariableValue(9));

        store.TryGetTimestamps(Key, out long stillCreatedAtMs, out _).Should().BeTrue();

        // "Age since created" must not restart every time the value moves.
        stillCreatedAtMs.Should().Be(createdAtMs);
    }

    [Fact]
    public async Task RemovingAValue_DropsItsTimes()
    {
        KeyValueStore store = new();

        await store.GiveValueAsync(Key, new WiredVariableValue(5));
        store.RemoveValue(Key).Should().BeTrue();

        store.TryGetTimestamps(Key, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void AStoreSavedBeforeTimesWereKept_StillLoads()
    {
        // Exactly what an older room wrote: values, no times.
        const string legacy = """{"Store":{"4242|1|7":{"Value":5}}}""";

        ExtraData extraData = new(null);

        extraData.UpdateSection(
            ExtraDataSectionType.STORAGE,
            JsonDocument.Parse(legacy).RootElement
        );

        KeyValueStore? reloaded = Reload(extraData.GetJsonString());

        reloaded.Should().NotBeNull();
        reloaded!.TryGetValue(Key, out WiredVariableValue value).Should().BeTrue();
        value.Value.Should().Be(5);

        // Unknown rather than the epoch: the age condition must not read these as ancient.
        reloaded.TryGetTimestamps(Key, out _, out _).Should().BeFalse();
    }

    private static KeyValueStore RoundTrip(KeyValueStore store)
    {
        ExtraData extraData = new(null);

        // Mirrors FurnitureWiredVariableLogic's persistence callback.
        extraData.UpdateSection(
            ExtraDataSectionType.STORAGE,
            JsonSerializer.SerializeToNode(store, store.GetType())
        );

        return Reload(extraData.GetJsonString())!;
    }

    private static KeyValueStore? Reload(string persisted)
    {
        ExtraData extraData = new(persisted);

        return extraData.TryGetSection(ExtraDataSectionType.STORAGE, out JsonElement element)
            ? element.Deserialize<KeyValueStore>()
            : null;
    }
}
