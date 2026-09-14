using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One of the six dynamic slots of the hotel view.
/// </summary>
/// <remarks>
/// A slot's <c>conf</c> key means two different things depending on <paramref name="Widget"/>, which
/// is the single most confusing thing about this file:
/// <list type="bullet">
/// <item><c>widgetcontainer</c> -- <c>conf</c> is a <b>schedule</b>, and the campaign it selects
/// supplies the real widget. That is <paramref name="Schedule"/>.</item>
/// <item><c>generic</c> -- <c>conf</c> is an <b>element list</b> rendered directly in the slot, with
/// no campaign and no schedule. That is <paramref name="Elements"/>.</item>
/// <item>anything else -- <c>conf</c> is unused.</item>
/// </list>
/// Both are carried so switching the widget type does not throw the other away before the operator
/// has saved.
/// </remarks>
public sealed record HotelViewSlot(
    int Number,
    string Widget,
    IReadOnlyList<HotelViewScheduleEntry> Schedule,
    IReadOnlyList<HotelViewElement> Elements,
    IReadOnlyList<HotelViewLayoutValue> Layout,
    bool Separator,
    string SeparatorTitle,
    bool Ignore
);
