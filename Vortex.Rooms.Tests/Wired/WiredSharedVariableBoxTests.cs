using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Storage;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// A variable box set to "Permanent, shared across rooms", writing where another room can read it.
/// </summary>
/// <remarks>
/// The third availability option was the one with nowhere to go: a room-active value lives in the
/// owning room's memory and a permanent one on its own furni row, and neither is visible from
/// another room. The box now keeps a replica over the grain that owns the variable, which is what
/// makes the setting mean anything at all.
/// </remarks>
public sealed class WiredSharedVariableBoxTests
{
    private const int BoxId = 11;

    private const int Player = 7;

    [Fact]
    public async Task ItsValuesGoToTheGrain_NotToTheRoom()
    {
        FakeSharedGrain grain = new();
        TestSharedUserVariable box = await BoxAsync(grain);

        (await box.GiveValueAsync(Key(box), new WiredVariableValue(5))).Should().BeTrue();

        grain
            .Values.Should()
            .ContainSingle()
            .Which.Value.Value.Should()
            .Be(5, "the room's own store cannot be seen from another room");
    }

    [Fact]
    public async Task ItReadsBackWhatItWrote()
    {
        TestSharedUserVariable box = await BoxAsync(new FakeSharedGrain());

        await box.GiveValueAsync(Key(box), new WiredVariableValue(5));

        box.TryGetValue(Key(box), out WiredVariableValue value).Should().BeTrue();
        value.Value.Should().Be(5);
    }

    /// <summary>
    /// What the other room wrote, once the copy is old enough to be refetched.
    /// </summary>
    /// <remarks>
    /// Tested on the store rather than through the box, because the interval is the behaviour: a
    /// box that refetched on every hydration would ask the grain far more often than a shared
    /// variable is worth, and one that never refetched would be a private copy wearing a shared
    /// name.
    /// </remarks>
    [Fact]
    public async Task ACopyThatHasAgedPicksUpWhatAnotherRoomWrote()
    {
        FakeSharedGrain grain = new();
        long now = 1_000;

        WiredSharedVariableStore store = new(grain, NullLogger.Instance, () => now);

        await store.RefreshAsync(CancellationToken.None);

        // Straight into the grain, the way the other room's box would.
        await grain.GiveAsync(StorageKey, 9, replace: true, CancellationToken.None);

        await store.RefreshAsync(CancellationToken.None);

        store
            .ContainsKey(RawKey)
            .Should()
            .BeFalse("the copy was refreshed a moment ago and is not asked again");

        now += 5_000;

        await store.RefreshAsync(CancellationToken.None);

        store.TryGetValue(RawKey, out WiredVariableValue value).Should().BeTrue();
        value.Value.Should().Be(9);
    }

    [Fact]
    public async Task AWriteTheGrainRefusesIsNotRecordedHere()
    {
        FakeSharedGrain grain = new() { Refuse = true };
        TestSharedUserVariable box = await BoxAsync(grain);

        (await box.GiveValueAsync(Key(box), new WiredVariableValue(5))).Should().BeFalse();

        box.TryGetValue(Key(box), out _)
            .Should()
            .BeFalse("reporting a write the owner refused would let two rooms disagree for good");
    }

    /// <summary>A key of the store's own, for the tests that exercise it without a box around it.
    /// </summary>
    private static readonly WiredVariableKey RawKey = new(
        WiredVariableId.Parse("4242"),
        WiredVariableTargetType.User,
        Player
    );

    private static readonly string StorageKey = RawKey.ToStorageKey();

    private static WiredVariableKey Key(TestSharedUserVariable box) =>
        new(box.GetVarSnapshot().VariableId, WiredVariableTargetType.User, Player);

    private static async Task<TestSharedUserVariable> BoxAsync(FakeSharedGrain grain)
    {
        TestSharedUserVariable box = new(
            WiredTestBoxes.Context(BoxId),
            Factory(grain),
            new WiredData
            {
                StringParam = "score",
                IntParams = [(int)WiredAvailabilityType.Shared, 1],
            }
        );

        await box.LoadWiredAsync(CancellationToken.None);

        return box;
    }

    private static IGrainFactory Factory(FakeSharedGrain grain) =>
        FakeProxy.Create<IGrainFactory>(call =>
            call.Method.Name == nameof(IGrainFactory.GetGrain) ? grain : null
        );

    private sealed class TestSharedUserVariable : WiredVariableUser
    {
        public TestSharedUserVariable(
            IRoomFloorItemContext ctx,
            IGrainFactory grainFactory,
            WiredData data
        )
            : base(grainFactory, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    /// <summary>The grain, in memory: one activation per variable is what the real one guarantees,
    /// and one instance per test is what that looks like from here.</summary>
    private sealed class FakeSharedGrain : IWiredSharedVariableGrain
    {
        public Dictionary<string, WiredSharedValueSnapshot> Values { get; } = [];

        /// <summary>Refuses every write, standing in for a database that would not take one.
        /// </summary>
        public bool Refuse { get; set; }

        public Task<ImmutableArray<WiredSharedValueSnapshot>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult(Values.Values.ToImmutableArray());

        public Task<bool> GiveAsync(
            string storageKey,
            int value,
            bool replace,
            CancellationToken ct
        )
        {
            if (Refuse || (Values.ContainsKey(storageKey) && !replace))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(Write(storageKey, value));
        }

        public Task<bool> SetAsync(string storageKey, int value, CancellationToken ct) =>
            Task.FromResult(!Refuse && Values.ContainsKey(storageKey) && Write(storageKey, value));

        public Task<bool> RemoveAsync(string storageKey, CancellationToken ct) =>
            Task.FromResult(Values.Remove(storageKey));

        private bool Write(string storageKey, int value)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Values[storageKey] = new WiredSharedValueSnapshot
            {
                StorageKey = storageKey,
                Value = value,
                CreatedAtMs = Values.TryGetValue(storageKey, out WiredSharedValueSnapshot? existing)
                    ? existing.CreatedAtMs
                    : now,
                UpdatedAtMs = now,
            };

            return true;
        }
    }
}
