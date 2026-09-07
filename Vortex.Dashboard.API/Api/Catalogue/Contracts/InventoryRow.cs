namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One table, counted.
/// </summary>
/// <param name="Route">Where an operator can go and look, or null for a table with no page yet --
/// which is what makes this list an audit of coverage rather than only of data.</param>
/// <param name="Empty">The same fact as a zero count, so the row can be styled without comparing.</param>
public sealed record InventoryRow(string Key, int Count, string? Route, bool Empty);
