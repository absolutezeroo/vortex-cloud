using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One domain's counts.</summary>
public sealed record InventoryGroup(string Key, IReadOnlyList<InventoryRow> Rows);
