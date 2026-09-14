namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// Where one of the six dynamic slots sits on screen, so the page can draw the grid rather than
/// describe it.
/// </summary>
/// <remarks>
/// The arrangement is hard-coded in the client, not configurable: <c>DynamicLayoutManager</c> builds
/// a top row, a centre pair and a bottom pair, and <c>GenericWidget.isWideSlot</c> decides the width
/// with a literal <c>slot != 3 &amp;&amp; slot != 5</c>. Slots 4 and 5 are the only ones that can
/// carry a titled separator; only slot 5 can be told to stop dictating the bottom row's height.
/// </remarks>
public sealed record HotelViewSlotShape(
    int Number,
    string Column,
    bool Wide,
    bool CanSeparate,
    bool CanIgnore
);
