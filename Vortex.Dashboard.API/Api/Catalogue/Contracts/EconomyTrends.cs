using System.Collections.Generic;
using Vortex.Dashboard.API.Api.Hotel.Contracts;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// Where the hotel's money went, per currency.
/// </summary>
/// <param name="Currencies">The currency names actually present in the ledger for this window, in
/// order. They are the ledger's own strings rather than an enum because a hotel renames its
/// currencies in <c>currency_types</c>, so the labels here are whatever that hotel calls them --
/// which is also why <paramref name="Totals"/> is keyed by them rather than having fields.</param>
public sealed record EconomyTrends(
    ReportWindow Window,
    IReadOnlyList<string> Currencies,
    IReadOnlyList<EconomyTrendSeries> Series,
    IReadOnlyDictionary<string, EconomyCurrencyTotals> Totals,
    IReadOnlyList<EconomySpendCategory> Categories
);
