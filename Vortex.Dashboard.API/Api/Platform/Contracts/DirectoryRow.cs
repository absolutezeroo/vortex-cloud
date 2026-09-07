namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One row of a directory whose rows are table rows.
/// </summary>
/// <param name="Id">The database key.</param>
/// <param name="Value">What a filter stores when this row is picked -- the id as a string, unless
/// the signal carries a code instead, which several of these directories do. It is the reason this
/// is a separate field and not something the picker derives.</param>
public sealed record DirectoryRow(int Id, string Value, string Name, string? Description);
