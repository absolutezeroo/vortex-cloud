using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api;
using Vortex.Database.Context;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The economy trend reads used to pull every ledger row in their window into the emulator's heap
/// and group it there. They now group in the database -- but "it groups in the database" is exactly
/// the kind of claim nothing else in this repository can check. The compiler is happy either way,
/// and an in-memory provider silently evaluates an untranslatable expression client-side, which both
/// hides a translation failure and reproduces the very behaviour the change removes.
///
/// <para>
/// So these tests ask the real MySQL provider to render the queries as SQL.
/// <see cref="RelationalQueryableExtensions.ToQueryString" /> needs no server: it stops at the
/// generated command. An expression the provider cannot translate throws here instead of on the
/// operator's first page load, and a GROUP BY that quietly moved back into memory shows up as a
/// missing clause.
/// </para>
/// </summary>
public sealed class DashboardEconomyQueryTranslationTests
{
    private static readonly DateTime SINCE = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UNTIL = new(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TheTrendQueryGroupsByDayAndCurrencyInSql()
    {
        using VortexDbContext db = OfflineContext();

        string sql = DashboardApiService.EconomyTrendQuery(db, SINCE, UNTIL).ToQueryString();

        sql.Should().Contain("GROUP BY");
        sql.Should().Contain("SUM(");
        sql.Should().Contain("COUNT(");
    }

    [Fact]
    public void TheSpendCategoryQueryGroupsAndLeftJoinsInSql()
    {
        using VortexDbContext db = OfflineContext();

        string sql = DashboardApiService.SpendCategoryQuery(db, SINCE, UNTIL).ToQueryString();

        sql.Should().Contain("GROUP BY");
        sql.Should().Contain("SUM(");
        sql.Should().Contain("LEFT JOIN");
    }

    /// <summary>
    /// The real provider, pinned to a server version so nothing tries to detect one, and a
    /// connection string that is never opened -- <c>ToQueryString</c> only needs the SQL generator.
    /// </summary>
    private static VortexDbContext OfflineContext() =>
        new(
            new DbContextOptionsBuilder<VortexDbContext>()
                .UseMySql(
                    "server=localhost;database=vortex_query_translation;user=none;password=none",
                    ServerVersion.Parse("8.0.36-mysql")
                )
                .Options
        );
}
