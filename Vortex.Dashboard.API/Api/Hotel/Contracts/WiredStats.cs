using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>What wired boxes are placed in the hotel, and where.</summary>
public sealed record WiredStats(
    WiredTotals Totals,
    IReadOnlyList<WiredCategoryCount> ByCategory,
    IReadOnlyList<WiredLogicCount> ByLogic,
    IReadOnlyList<WiredRoomCount> TopRooms
);
