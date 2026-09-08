using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// What the item journal says looks wrong, over one window.
/// </summary>
/// <param name="Since">The window scanned. Unbounded is not offered: <c>item_events</c> grows
/// forever, and a scan with no window is a table lock waiting for a busy afternoon.</param>
/// <param name="ItemsScanned">How many distinct items the window covered, so a result of nothing
/// can be read as "nothing wrong here" rather than "nothing looked at".</param>
public sealed record ItemAnomalyScan(
    DateTime Since,
    DateTime Until,
    int ItemsScanned,
    int Count,
    IReadOnlyList<ItemAnomaly> Items
);
