using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Which variable changes the "variable changed" trigger fires on. Three checkboxes, and a nested
/// group of three under the middle one sent as a bit mask — a trigger that ignored the mask would
/// fire on every write when the player asked only for increases.
/// </summary>
public sealed class WiredVariableChangeMatcherTests
{
    private const int Increased = 1 << 0;

    private const int Decreased = 1 << 1;

    private const int Unchanged = 1 << 2;

    [Fact]
    public void EachKind_FiresOnlyWhenItsBoxIsTicked()
    {
        Match(WiredVariableChangeKind.Created, onCreated: true).Should().BeTrue();
        Match(WiredVariableChangeKind.Created).Should().BeFalse();

        Match(WiredVariableChangeKind.Deleted, onDeleted: true).Should().BeTrue();
        Match(WiredVariableChangeKind.Deleted).Should().BeFalse();

        Match(WiredVariableChangeKind.ValueChanged, onValueChanged: true).Should().BeTrue();
        Match(WiredVariableChangeKind.ValueChanged).Should().BeFalse();
    }

    [Theory]
    [InlineData(Increased, 1, 5, true)]
    [InlineData(Increased, 5, 1, false)]
    [InlineData(Decreased, 5, 1, true)]
    [InlineData(Decreased, 1, 5, false)]
    [InlineData(Unchanged, 5, 5, true)]
    [InlineData(Unchanged, 1, 5, false)]
    [InlineData(Increased | Decreased, 5, 1, true)]
    [InlineData(Increased | Decreased, 5, 5, false)]
    public void TheNestedMask_FiltersTheDirection(
        int subMask,
        int previous,
        int current,
        bool expected
    ) =>
        WiredVariableChangeMatcher
            .Matches(
                WiredVariableChangeKind.ValueChanged,
                previous,
                current,
                onCreated: false,
                onValueChanged: true,
                onDeleted: false,
                subMask
            )
            .Should()
            .Be(expected);

    [Fact]
    public void AnEmptyMask_MeansAnyDirection()
    {
        // "Value changed" ticked with none of its three nested options is an unrestricted ask, not
        // an impossible one.
        Match(WiredVariableChangeKind.ValueChanged, onValueChanged: true, previous: 1, current: 5)
            .Should()
            .BeTrue();

        Match(WiredVariableChangeKind.ValueChanged, onValueChanged: true, previous: 5, current: 5)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void TheMask_DoesNotGateCreationOrDeletion()
    {
        // The nested group hangs off "Value changed" only; a creation ticked as wanted must fire
        // whatever the mask says.
        WiredVariableChangeMatcher
            .Matches(
                WiredVariableChangeKind.Created,
                previous: 0,
                current: 5,
                onCreated: true,
                onValueChanged: false,
                onDeleted: false,
                Decreased
            )
            .Should()
            .BeTrue();
    }

    private static bool Match(
        WiredVariableChangeKind kind,
        bool onCreated = false,
        bool onValueChanged = false,
        bool onDeleted = false,
        int previous = 0,
        int current = 1
    ) =>
        WiredVariableChangeMatcher.Matches(
            kind,
            previous,
            current,
            onCreated,
            onValueChanged,
            onDeleted,
            0
        );
}
