using System.Collections.Generic;
using Vortex.Dashboard.API.Api.Hotel.Contracts;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>What the player-to-player marketplace did over the window.</summary>
public sealed record MarketplaceSummary(
    ReportWindow Window,
    MarketplaceTotals Totals,
    IReadOnlyList<MarketplaceSalePoint> Timeline,
    IReadOnlyList<MarketplaceSeller> TopSellers
);
