using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The two economy aggregates that used to be computed in the emulator's own heap. Both read the
/// ledger, the fastest-growing table there is, and both were <c>ToListAsync</c> followed by a LINQ
/// <c>GroupBy</c> -- so a wide window materialised every row it covered inside the game process,
/// where the GC pause is a gameplay pause.
///
/// <para>
/// They live here, returning <see cref="IQueryable{T}" /> rather than results, for one reason: an
/// expression the provider cannot translate compiles perfectly and fails at the first request, and
/// an in-memory test provider evaluates it client-side and reports success. Handing the query back
/// lets <c>DashboardEconomyQueryTranslationTests</c> ask the real MySQL provider to render it as
/// SQL, which is the only cheap way to know the grouping actually happens in the database.
/// </para>
/// </summary>
internal sealed partial class DashboardApiService
{
    /// <summary>
    /// One row per (day, currency) with spend, earnings and transaction count already summed. Day is
    /// the finest bucket the charts offer, so the month and year granularities fold up from this
    /// same result without a second query.
    /// </summary>
    internal static IQueryable<EconomyTrendRow> EconomyTrendQuery(
        VortexDbContext db,
        DateTime since,
        DateTime until
    ) =>
        db
            .EconomyLedger.AsNoTracking()
            .Where(l => l.OccurredAt >= since && l.OccurredAt <= until)
            .GroupBy(l => new { Day = l.OccurredAt.Date, l.Currency })
            .Select(g => new EconomyTrendRow(
                g.Key.Day,
                g.Key.Currency,
                g.Sum(l => l.Delta < 0 ? -l.Delta : 0L),
                g.Sum(l => l.Delta > 0 ? l.Delta : 0L),
                g.Count()
            ));

    /// <summary>
    /// Spend per (currency, originating action). The ledger's own <c>Reason</c> is only
    /// Debit/Grant/Adjustment, so the business context comes from the audit event that shares the
    /// operation's correlation id -- a left join, because a debit with no audited action is spend
    /// that still has to be reported, as <c>uncategorized</c>.
    /// </summary>
    internal static IQueryable<EconomySpendCategoryRow> SpendCategoryQuery(
        VortexDbContext db,
        DateTime since,
        DateTime until
    ) =>
        from l in db.EconomyLedger.AsNoTracking()
        where
            l.OccurredAt >= since && l.OccurredAt <= until && l.Delta < 0 && l.CorrelationId != null
        join a in db.AuditEvents.AsNoTracking()
            on l.CorrelationId equals a.CorrelationId
            into matched
        from a in matched.DefaultIfEmpty()
        group l by new { l.Currency, Action = a != null ? a.Action : null } into g
        select new EconomySpendCategoryRow(
            g.Key.Currency,
            g.Key.Action,
            g.Sum(l => -l.Delta),
            g.Count()
        );
}
