using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One currency's line on the chart.</summary>
public sealed record EconomyTrendSeries(string Currency, IReadOnlyList<EconomyTrendPoint> Points);
