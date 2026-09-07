namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The whole bot population, not the window.
/// </summary>
/// <param name="ConfiguredBots">Bots with any skill at all. The counts below narrow that: a bot can
/// be configured and still silent, which is the gap this page exists to show.</param>
public sealed record BotTotals(
    int TotalBots,
    int PlacedBots,
    int InventoryBots,
    int ConfiguredBots,
    int ChattyBots,
    int AutoChatBots,
    int WanderingBots,
    int DancingBots,
    int DistinctOwners,
    int RoomsWithBots
);
