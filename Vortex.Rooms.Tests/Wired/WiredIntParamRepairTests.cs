using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// A wired box hydrating from <c>extra_data</c> must always end up with one int param per rule.
/// The live symptom of it not doing so: freshly placed "move &amp; rotate" boxes persisted an empty
/// list, so <c>WiredActionMoveRotateFurni.FillInternalDataAsync</c> threw
/// <c>ArgumentOutOfRangeException</c> out of <c>WiredData.GetIntParam</c> on every fire.
/// </summary>
public sealed class WiredIntParamRepairTests
{
    // The move & rotate box: movement type 0..11, rotation type 0..3, no tail rule.
    private static readonly List<IWiredParamRule> MoveRotateRules =
    [
        new WiredRangeParamRule(0, 11, 0),
        new WiredRangeParamRule(0, 3, 0),
    ];

    [Fact]
    public void NeverConfiguredBox_GetsOneDefaultPerRule()
    {
        List<int> repaired = WiredIntParamRepair.Repair(MoveRotateRules, null, 16, []);

        repaired.Should().Equal(0, 0);
    }

    [Fact]
    public void ShortList_IsPaddedWithDefaults_KeepingConfiguredSlots()
    {
        List<int> repaired = WiredIntParamRepair.Repair(
            [new WiredRangeParamRule(0, 11, 0), new WiredRangeParamRule(0, 3, 2)],
            null,
            16,
            [9]
        );

        repaired.Should().Equal(9, 2);
    }

    [Fact]
    public void OutOfRangeSlot_FallsBackToItsDefault_WithoutDroppingTheOthers()
    {
        List<int> repaired = WiredIntParamRepair.Repair(MoveRotateRules, null, 16, [99, 3]);

        repaired.Should().Equal(0, 3);
    }

    [Fact]
    public void ValidList_IsReturnedUnchanged()
    {
        List<int> repaired = WiredIntParamRepair.Repair(MoveRotateRules, null, 16, [11, 3]);

        repaired.Should().Equal(11, 3);
    }

    [Fact]
    public void TailEntries_AreSanitizedInPlace_NotDropped()
    {
        // Leaves that read the tail as a positional bit mask break if an invalid entry shifts the
        // ones after it, so a bad tail value becomes the tail rule's default and keeps its slot.
        List<int> repaired = WiredIntParamRepair.Repair(
            [new WiredRangeParamRule(0, 11, 0)],
            new WiredBoolParamRule(false),
            16,
            [4, 1, 7, 1]
        );

        repaired.Should().Equal(4, 1, 0, 1);
    }

    [Fact]
    public void TailEntries_AreCappedAtTheConfiguredMaximum()
    {
        List<int> repaired = WiredIntParamRepair.Repair(
            [new WiredRangeParamRule(0, 11, 0)],
            new WiredBoolParamRule(false),
            3,
            [4, 1, 1, 1, 1, 1]
        );

        repaired.Should().Equal(4, 1, 1);
    }
}
