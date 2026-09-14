using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One of the element types a <c>generic</c> landing-view widget can stack in its content column.
/// </summary>
/// <remarks>
/// The list is closed: <c>createHandler</c> in the client returns <see langword="null"/> for anything
/// else, and <c>GenericWidget.configureContentColumn</c> then <b>abandons the whole column</b> rather
/// than skipping the bad entry -- one typo and the widget renders empty.
/// </remarks>
/// <param name="Verified">
/// Whether every argument below was read off the client's handler. <see langword="false"/> means the
/// type exists but its arguments are not all understood; the page edits those as a raw comma list
/// instead of pretending to know. Unknown official behaviour stays explicitly unknown.
/// </param>
public sealed record HotelViewElementType(
    string Type,
    IReadOnlyList<HotelViewArgument> Arguments,
    bool Verified
);
