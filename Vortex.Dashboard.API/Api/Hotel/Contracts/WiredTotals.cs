namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>The two numbers that say whether wired is used at all.</summary>
public sealed record WiredTotals(int TotalWiredPlaced, int RoomsWithWired);
