using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One limited-edition series and how far through it the hotel is.
/// </summary>
/// <param name="ProductName">The furniture the series hands out. Null when the product row is gone,
/// which leaves a series that can no longer pay out.</param>
/// <param name="Running">Active, not yet raffled, and inside its dates -- the four conditions an
/// operator would otherwise check by eye across four columns.</param>
/// <param name="PendingEntries">Raffle entries not yet processed.</param>
public sealed record LtdSeriesRow(
    int Id,
    int ProductId,
    string? ProductName,
    string? IconUrl,
    int TotalQuantity,
    int RemainingQuantity,
    int Sold,
    int CostCredits,
    int RaffleWindowSeconds,
    bool IsActive,
    bool HasRaffleFinished,
    DateTime? StartsAt,
    DateTime? EndsAt,
    bool Running,
    int PendingEntries,
    IReadOnlyList<LtdRaffleResultCount> EntriesByResult
);
