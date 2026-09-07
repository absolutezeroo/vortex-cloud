namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One row of a directory built from distinct codes -- a badge nobody was granted and a campaign no
/// quest belongs to are both filters nobody can satisfy, so these lists are what exists rather than
/// what is configured.
/// </summary>
/// <param name="Id">The code itself: there is no table row behind it to have a key. This is why the
/// picker's row id is a string as well as a number, and the two cannot be collapsed.</param>
public sealed record CodeDirectoryRow(string Id, string Value, string Name, string? Description);
