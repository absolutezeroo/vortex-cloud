namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>Which filter produced this page. Each is null when it was not given.</summary>
public sealed record ChatlogFilters(string? Q, int? Player, int? Room);
