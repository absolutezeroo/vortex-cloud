using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The languages the client is told about.
/// </summary>
/// <remarks>
/// The list comes from <c>external_variables.json</c> -- that block IS the state, so there is no
/// flag in a database that could drift from it.
/// </remarks>
public sealed record GamedataLanguageList(
    bool Available,
    DateTime? ModifiedUtc,
    IReadOnlyList<GamedataLanguageRow> Languages
);
