using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Vortex.Players.Effects;
using Vortex.Primitives.Inventory.Snapshots;
using Xunit;

namespace Vortex.Players.Tests.Effects;

public class AvatarEffectListBuilderTests
{
    private static readonly DateTime Now = new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_EmptyInput_ReturnsEmpty()
    {
        AvatarEffectListBuilder.Build([], Now).Should().BeEmpty();
    }

    [Fact]
    public void Build_PermanentEffect_IsPermanentAndZeroDuration()
    {
        ImmutableArray<AvatarEffectSnapshot> result = AvatarEffectListBuilder.Build(
            [new PlayerEffectRow(10, 0, 0, ActivatedAt: null, IsSelected: false)],
            Now
        );

        AvatarEffectSnapshot effect = result.Should().ContainSingle().Subject;
        effect.Type.Should().Be(10);
        effect.Duration.Should().Be(0);
        effect.IsPermanent.Should().BeTrue();
        effect.InactiveEffectsInInventory.Should().Be(1);
        effect.SecondsLeftIfActive.Should().Be(0);
    }

    [Fact]
    public void Build_ActivatedTimedEffect_ReportsRemainingSeconds()
    {
        ImmutableArray<AvatarEffectSnapshot> result = AvatarEffectListBuilder.Build(
            [new PlayerEffectRow(5, 0, 600, ActivatedAt: Now.AddSeconds(-100), IsSelected: true)],
            Now
        );

        AvatarEffectSnapshot effect = result.Should().ContainSingle().Subject;
        effect.IsPermanent.Should().BeFalse();
        effect.Duration.Should().Be(600);
        effect.SecondsLeftIfActive.Should().Be(500);
        effect.InactiveEffectsInInventory.Should().Be(0);
    }

    [Fact]
    public void Build_ExpiredTimedEffect_ClampsRemainingToZero()
    {
        ImmutableArray<AvatarEffectSnapshot> result = AvatarEffectListBuilder.Build(
            [new PlayerEffectRow(7, 0, 60, ActivatedAt: Now.AddSeconds(-120), IsSelected: false)],
            Now
        );

        result.Should().ContainSingle().Which.SecondsLeftIfActive.Should().Be(0);
    }

    [Fact]
    public void Build_StacksSameEffect_CountsOnlyInactiveCopies()
    {
        ImmutableArray<AvatarEffectSnapshot> result = AvatarEffectListBuilder.Build(
            [
                new PlayerEffectRow(9, 0, 300, ActivatedAt: Now.AddSeconds(-60), IsSelected: false),
                new PlayerEffectRow(9, 0, 300, ActivatedAt: null, IsSelected: false),
                new PlayerEffectRow(9, 0, 300, ActivatedAt: null, IsSelected: false),
            ],
            Now
        );

        AvatarEffectSnapshot effect = result.Should().ContainSingle().Subject;
        effect.Type.Should().Be(9);
        effect.InactiveEffectsInInventory.Should().Be(2);
        effect.SecondsLeftIfActive.Should().Be(240);
    }

    [Fact]
    public void Build_DistinctEffects_AreGroupedAndOrdered()
    {
        ImmutableArray<AvatarEffectSnapshot> result = AvatarEffectListBuilder.Build(
            [
                new PlayerEffectRow(30, 0, 0, ActivatedAt: null, IsSelected: false),
                new PlayerEffectRow(4, 1, 0, ActivatedAt: null, IsSelected: false),
                new PlayerEffectRow(4, 0, 0, ActivatedAt: null, IsSelected: false),
            ],
            Now
        );

        result.Select(e => (e.Type, e.SubType)).Should().Equal((4, 0), (4, 1), (30, 0));
    }
}
