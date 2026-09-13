using System;
using FluentAssertions;
using Vortex.PacketHandlers.Competition;
using Xunit;

namespace Vortex.PacketHandlers.Tests;

/// <summary>
/// The schedule strings below are the ones the client actually ships in
/// `../vortex-modern-client/sources/external_variables.txt`.
/// </summary>
public sealed class GetCurrentTimingCodeMessageHandlerTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 0, 0, 0, DateTimeKind.Unspecified);

    [Theory]
    // landing.view.dynamic.slot.2.conf -- both dates passed, the later entry wins.
    [InlineData("2026-01-08 12:00,jan26cf;2026-01-13 12:00,jul23bb", "jul23bb")]
    // landing.view.dynamic.slot.4.conf -- three entries, still the latest passed one.
    [InlineData(
        "2026-01-09 12:00,jan26r2;2026-01-12 12:00,sep21diamond;2026-01-13 12:00,jan26fam",
        "jan26fam"
    )]
    // competition.timing -- an empty tail code switches the campaign back off.
    [InlineData("2013-03-07 10:00,steamRoomComp;2013-03-21 11:00,", "")]
    // Nothing has started yet.
    [InlineData("2027-01-01 00:00,future", "")]
    // Single entry, no separator.
    [InlineData("2026-01-08 12:00,jan26cf", "jan26cf")]
    // Unparseable and malformed entries are skipped, not fatal.
    [InlineData("garbage;2026-01-08 12:00,jan26cf;nodatehere", "jan26cf")]
    [InlineData("", "")]
    public void ResolveCode_PicksTheLatestEntryWhoseDateHasPassed(string schedule, string expected)
    {
        GetCurrentTimingCodeMessageHandler.ResolveCode(schedule, Now).Should().Be(expected);
    }
}
