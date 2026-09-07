using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One gamedata file, with what the page needs to show it and to save it safely.
/// </summary>
/// <param name="Parses">False for a file on disk that is not valid JSON. It is still listed --
/// a file the server cannot read is exactly the one an operator has to be told about.</param>
/// <param name="ModifiedUtc">Travels back on every write as the caller's expected value: it is
/// what turns two operators editing at once into a refusal rather than a dropped edit.</param>
/// <param name="Categories">Filled for furnidata only, so the category filter offers what the file
/// actually contains instead of a free-text box nobody can spell.</param>
public sealed record GamedataFileRow(
    string File,
    string Name,
    bool Localised,
    int Entries,
    bool Parses,
    DateTime? ModifiedUtc,
    IReadOnlyList<string> Categories
);
