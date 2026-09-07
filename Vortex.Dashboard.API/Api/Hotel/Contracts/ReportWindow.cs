using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The period a report covers and how it is bucketed, echoed back so a chart can label itself
/// without re-deriving what it asked for.
/// </summary>
public sealed record ReportWindow(DateTime Since, DateTime Until, string Granularity);
