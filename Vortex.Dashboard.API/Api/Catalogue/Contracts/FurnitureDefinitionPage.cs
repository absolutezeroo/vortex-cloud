using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One page of the furniture-definition admin list.</summary>
public sealed record FurnitureDefinitionPage(
    int Page,
    int Limit,
    int Offset,
    int Total,
    int Count,
    IReadOnlyList<FurnitureDefinitionRow> Items
);
