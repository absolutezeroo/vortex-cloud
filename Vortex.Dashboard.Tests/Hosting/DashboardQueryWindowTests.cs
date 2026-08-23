using System;
using System.Collections.Specialized;
using FluentAssertions;
using Vortex.Dashboard.API.Api;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The windowed reads pull their whole window into memory to bucket it, so the two things that must
/// not happen quietly are an unparsable date (dropping the filter widens the query) and an unbounded
/// span (?since=2010-01-01 over the economy ledger). Both are 400s now, and this is what says so.
/// </summary>
public sealed class DashboardQueryWindowTests
{
    private static readonly DateTime NOW = new(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

    private static NameValueCollection Query(string? since = null, string? until = null)
    {
        NameValueCollection query = new();

        if (since is not null)
        {
            query.Add("since", since);
        }

        if (until is not null)
        {
            query.Add("until", until);
        }

        return query;
    }

    [Fact]
    public void DefaultsToTheLastThirtyDays()
    {
        (DateTime since, DateTime until) = DashboardApiService.ResolveWindow(Query(), NOW);

        until.Should().Be(NOW);
        since.Should().Be(NOW.AddDays(-30));
    }

    [Fact]
    public void HonoursAnExplicitDefaultSpan()
    {
        (DateTime since, _) = DashboardApiService.ResolveWindow(
            Query(),
            NOW,
            TimeSpan.FromHours(24)
        );

        since.Should().Be(NOW.AddHours(-24));
    }

    [Fact]
    public void SwapsAnInvertedPair()
    {
        (DateTime since, DateTime until) = DashboardApiService.ResolveWindow(
            Query(since: "2026-08-20T00:00:00Z", until: "2026-08-10T00:00:00Z"),
            NOW
        );

        since.Should().BeBefore(until);
    }

    [Fact]
    public void RefusesAWindowWiderThanAYear()
    {
        Action act = () =>
            DashboardApiService.ResolveWindow(
                Query(since: "2010-01-01T00:00:00Z", until: "2026-08-23T00:00:00Z"),
                NOW
            );

        act.Should().Throw<DashboardQueryException>().Which.Error.Should().Be("window_too_large");
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-13-45")]
    public void RefusesAnUnparsableDateInsteadOfDroppingTheFilter(string value)
    {
        Action act = () => DashboardApiService.ResolveWindow(Query(since: value), NOW);

        act.Should().Throw<DashboardQueryException>().Which.Error.Should().Be("invalid_date");
    }

    [Fact]
    public void TreatsAnAbsentValueAsNoFilter()
    {
        DashboardApiService.ParseDateTime(null).Should().BeNull();
        DashboardApiService.ParseDateTime("   ").Should().BeNull();
    }
}
