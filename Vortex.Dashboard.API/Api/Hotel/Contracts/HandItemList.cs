using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The whole hand-item table at once: a row exists only for a consumable, so what is missing is as
/// much of the answer as what is present.
/// </summary>
/// <param name="ImageTemplate">The URL pattern for an item's picture, so the editor can preview an
/// id that has no row yet -- which is every id being added for the first time.</param>
public sealed record HandItemList(
    int Count,
    int ConsumableCount,
    string? ImageTemplate,
    IReadOnlyList<HandItemRow> Items
);
