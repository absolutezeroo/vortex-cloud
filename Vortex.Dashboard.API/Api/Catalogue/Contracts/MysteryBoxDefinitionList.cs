using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>The furniture the client will offer the box dialog on.</summary>
public sealed record MysteryBoxDefinitionList(
    int Count,
    IReadOnlyList<MysteryBoxDefinitionRow> Items
);
