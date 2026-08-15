using System.Collections.Generic;
using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The cache a wired condition warms asynchronously and then reads synchronously. Two properties
/// matter: an expired entry must read as a miss (a condition must not pass on a stale roster), and a
/// fresh entry must suppress the refresh (a periodic stack would otherwise query every tick).
/// </summary>
public sealed class WiredTtlCacheTests
{
    [Fact]
    public void FreshEntry_IsReadable_AndSuppressesTheRefresh()
    {
        WiredTtlCache<int, HashSet<int>> cache = new(1_000);

        cache.Set(5, [1, 2], 10_000);

        cache.IsFresh(5, 10_500).Should().BeTrue();
        cache.TryGet(5, 10_500, out HashSet<int>? value).Should().BeTrue();
        value.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public void ExpiredEntry_ReadsAsAMiss()
    {
        WiredTtlCache<int, HashSet<int>> cache = new(1_000);

        cache.Set(5, [1], 10_000);

        cache.IsFresh(5, 11_001).Should().BeFalse();
        cache.TryGet(5, 11_001, out HashSet<int>? value).Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void UnknownKey_IsAMiss_NotAnEmptyHit()
    {
        WiredTtlCache<int, HashSet<int>> cache = new(1_000);

        cache.IsFresh(9, 0).Should().BeFalse();
        cache.TryGet(9, 0, out HashSet<int>? _).Should().BeFalse();
    }

    [Fact]
    public void Set_OverwritesAndRestartsTheClock()
    {
        WiredTtlCache<int, HashSet<int>> cache = new(1_000);

        cache.Set(5, [1], 10_000);
        cache.Set(5, [2], 10_900);

        cache.IsFresh(5, 11_500).Should().BeTrue();
        cache.TryGet(5, 11_500, out HashSet<int>? value).Should().BeTrue();
        value.Should().BeEquivalentTo([2]);
    }

    [Fact]
    public void Clear_DropsEverything()
    {
        WiredTtlCache<int, HashSet<int>> cache = new(1_000);

        cache.Set(5, [1], 10_000);
        cache.Clear();

        cache.IsFresh(5, 10_100).Should().BeFalse();
    }
}
