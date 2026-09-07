namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One row of whichever file is open.
/// </summary>
/// <remarks>
/// Three shapes in one record: a key/value line for variables and texts, a furniture for furnidata,
/// a product for productdata. Which fields are filled follows from the file, and the page already
/// knows which file it asked for -- it renders a different table per tab. Three records would be
/// three unions to narrow at every cell and would say nothing the tab does not already say.
/// </remarks>
/// <param name="Index">The position in the file, not the id: 5 ids are duplicated inside
/// roomitemtypes and 577 are shared with the wall list, so only the position addresses a row.
/// </param>
public sealed record GamedataEntry(
    string? Key,
    string? Value,
    string? Kind,
    int? Index,
    string? Id,
    string? Classname,
    string? Name,
    string? Description,
    string? Category,
    string? Xdim,
    string? Ydim,
    string? IconUrl,
    string? Code
);
