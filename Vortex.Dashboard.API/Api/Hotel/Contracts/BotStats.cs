using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>How many bots exist, how many are actually doing anything, and who owns them.</summary>
public sealed record BotStats(
    ReportWindow Window,
    BotTotals Totals,
    IReadOnlyList<BotGenderCount> ByGender,
    IReadOnlyList<BotGrowthPoint> Growth,
    IReadOnlyList<BotOwnerCount> TopOwners,
    IReadOnlyList<BotRoomCount> TopRooms
);
