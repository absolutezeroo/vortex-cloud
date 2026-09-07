namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>Keys of one colour, held against spent. Both are all-time, not the window.</summary>
public sealed record MysteryKeyColorCount(string Color, int Held, int Spent);
