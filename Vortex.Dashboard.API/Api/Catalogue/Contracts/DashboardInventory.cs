using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// Every table the dashboard knows about and how many rows are in it.
/// </summary>
/// <remarks>
/// Shaped as groups of {key, count, route} so the panel is a plain list the front end does not have
/// to keep in step with the read that builds it.
/// </remarks>
public sealed record DashboardInventory(DateTime GeneratedAt, IReadOnlyList<InventoryGroup> Groups);
