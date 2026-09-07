namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// The competing weight for one (pool, colour).
/// </summary>
/// <remarks>
/// The odds an operator cares about are per pool and per colour, and only mean anything against the
/// entries that can be drawn together -- so the denominator is computed here rather than guessed at
/// on the page.
/// </remarks>
public sealed record MysteryBoxPoolOdds(string Pool, string? Color, int TotalWeight, int Entries);
