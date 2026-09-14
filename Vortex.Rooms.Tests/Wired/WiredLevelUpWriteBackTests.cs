using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Wired.LevelUp;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Writing a level-up reading, which is reading it backwards.
/// </summary>
/// <remarks>
/// The add-on's readings used to refuse every write, so all eight came up greyed in the "change
/// variable value" box — that dialog filters its picker on <c>canWriteValue</c> and greys, rather
/// than hides, whatever fails. Five of them do have an inverse and are now writable, which means the
/// arithmetic of those inverses is load-bearing: a level set to 5 that reads back as 4 is a chain
/// that loops forever.
/// <para>
/// So the property under test is the round trip — write what the reading just said, and it must say
/// the same thing again. It is checked across the whole curve rather than at a couple of points,
/// because the interesting values are the boundaries (a level's first and last XP, the top of the
/// curve) and those are exactly the ones a hand-picked example misses.
/// </para>
/// </remarks>
public sealed class WiredLevelUpWriteBackTests
{
    private const int Step = 100;

    private const int MaxLevel = 25;

    /// <summary>
    /// Every reading with an inverse, round-tripped: read it, write that back, read it again.
    /// </summary>
    /// <remarks>
    /// <c>progress_percentage</c> is left out and has its own case below. It is the one reading that
    /// genuinely cannot round-trip in general — a percentage of a level worth three XP has only four
    /// distinct values, so the information is gone before the inverse ever runs. Asserting an
    /// identity that arithmetic forbids would only invite someone to "fix" the inverse.
    /// </remarks>
    [Theory]
    [InlineData("current_level")]
    [InlineData("current_xp")]
    [InlineData("progress")]
    [InlineData("xp_remaining")]
    public async Task WritingBackWhatAReadingJustSaidLeavesItSaying(string name)
    {
        WiredLinearLevelUpCurve curve = new(Step, MaxLevel);
        FakeParentVariable parent = new();
        WiredLevelUpSubVariable reading = Reading(name, parent, curve);

        for (int xp = 0; xp <= curve.MaxXp; xp += 7)
        {
            parent.Value = xp;

            reading.TryGetValue(Key, out WiredVariableValue before).Should().BeTrue();
            (await reading.SetValueAsync(null!, Key, before)).Should().BeTrue();
            reading.TryGetValue(Key, out WiredVariableValue after).Should().BeTrue();

            after
                .Value.Should()
                .Be(
                    before.Value,
                    "{0} was {1} at {2} XP and must still be after writing it back",
                    name,
                    before.Value,
                    xp
                );
        }
    }

    /// <summary>The three properties a builder actually leans on when writing a percentage.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, Step / 2)]
    [InlineData(100, Step)]
    public async Task WritingAPercentagePutsTheAvatarThatFarIntoTheLevel(int percent, int expected)
    {
        WiredLinearLevelUpCurve curve = new(Step, MaxLevel);
        FakeParentVariable parent = new() { Value = (Step * 4) + 37 };
        WiredLevelUpSubVariable reading = Reading("progress_percentage", parent, curve);

        (await reading.SetValueAsync(null!, Key, WiredVariableValue.Parse(percent)))
            .Should()
            .BeTrue();

        parent
            .Value.Should()
            .Be(
                curve.XpForLevel(5) + expected,
                "100% is the whole of the level, which is where the next one begins"
            );
    }

    /// <summary>
    /// A reading with no inverse must refuse, AND must not claim it can be written — the flag is the
    /// only thing the dialog looks at, so declaring it without the write is what puts a dead row in
    /// front of the builder.
    /// </summary>
    [Theory]
    [InlineData("xp_required")]
    [InlineData("is_maxed")]
    [InlineData("max_level")]
    public async Task AReadingWithNoInverseRefusesAndSaysSoInItsFlags(string name)
    {
        WiredLinearLevelUpCurve curve = new(Step, MaxLevel);
        FakeParentVariable parent = new() { Value = 250 };
        WiredLevelUpSubVariable reading = Reading(name, parent, curve);

        reading.GetVarSnapshot().Flags.Has(WiredVariableFlags.CanWriteValue).Should().BeFalse();

        (await reading.SetValueAsync(null!, Key, WiredVariableValue.Parse(1))).Should().BeFalse();
        parent.Value.Should().Be(250, "a refused write must not have moved anything");
    }

    /// <summary>
    /// The five that are writable are the five with an inverse, named — the add-on's table and the
    /// flags cannot drift apart without this failing.
    /// </summary>
    [Fact]
    public void TheWritableReadingsAreExactlyTheOnesWithAnInverse()
    {
        List<string> writable = [];

        foreach (
            (
                string name,
                _,
                Func<WiredLevelUpCurve, int, int, int>? write
            ) in WiredAddonVariableLevelUp.Readings
        )
        {
            if (write is not null)
            {
                writable.Add(name);
            }
        }

        writable
            .Should()
            .BeEquivalentTo([
                "current_level",
                "current_xp",
                "progress",
                "progress_percentage",
                "xp_remaining",
            ]);
    }

    private static readonly WiredVariableKey Key = new(
        WiredVariableId.Parse("42"),
        WiredVariableTargetType.Global,
        0
    );

    /// <summary>One reading, built from the add-on's own table so the test cannot hold a second copy
    /// of the arithmetic it is checking.</summary>
    private static WiredLevelUpSubVariable Reading(
        string name,
        IWiredVariable parent,
        WiredLevelUpCurve curve
    )
    {
        foreach (
            (
                string readingName,
                Func<WiredLevelUpCurve, int, int> read,
                Func<WiredLevelUpCurve, int, int, int>? write
            ) in WiredAddonVariableLevelUp.Readings
        )
        {
            if (readingName == name)
            {
                return new WiredLevelUpSubVariable(
                    parent,
                    Key.VariableId,
                    $"xp.{name}",
                    curve,
                    read,
                    write
                );
            }
        }

        throw new ArgumentOutOfRangeException(nameof(name), name, "No such level-up reading.");
    }

    /// <summary>The variable box the add-on is stacked on: one number, written straight through.</summary>
    private sealed class FakeParentVariable : IWiredVariable
    {
        public int Value { get; set; }

        public bool CanBind(in WiredVariableKey key) => true;

        public bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
        {
            value = WiredVariableValue.Parse(Value);

            return true;
        }

        public Task<bool> SetValueAsync(
            IWiredExecutionContext ctx,
            WiredVariableKey key,
            WiredVariableValue value
        )
        {
            Value = value.Value;

            return Task.FromResult(true);
        }

        public Task<bool> GiveValueAsync(
            WiredVariableKey key,
            WiredVariableValue value,
            bool replace = false
        ) => Task.FromResult(false);

        public bool RemoveValue(WiredVariableKey key) => false;

        public bool TryGetTimestamps(
            in WiredVariableKey key,
            out long createdAtMs,
            out long updatedAtMs
        )
        {
            createdAtMs = 0;
            updatedAtMs = 0;

            return false;
        }

        public WiredVariableSnapshot GetVarSnapshot() =>
            new()
            {
                VariableId = Key.VariableId,
                VariableName = "xp",
                VariableType = WiredVariableType.Created,
                VariableHash = default,
                AvailabilityType = WiredAvailabilityType.Internal,
                TargetType = WiredVariableTargetType.Global,
                Flags = WiredVariableFlags.HasValue | WiredVariableFlags.CanWriteValue,
                TextConnectors = [],
            };
    }
}
