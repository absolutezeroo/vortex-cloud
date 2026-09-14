using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The settings that apply to the hotel view as a whole rather than to one slot.
/// </summary>
/// <remarks>
/// Two of these look like styling and are not:
/// <list type="bullet">
/// <item>The colours are only applied when they <b>differ from the client's defaults</b> -- black
/// text, white etching, etching at the bottom. Setting one back to its default value does not
/// restore a default, it stops the client from touching the property at all, which is the same thing
/// only by coincidence.</item>
/// <item><paramref name="LayoutXml"/> chooses the whole window layout. The default is
/// <c>landing_view_default_dynamic_layout</c>, and it is the only one with the dynamic slot grid:
/// naming any other layout here makes every slot on this page inert.</item>
/// </list>
/// <paramref name="SceneSchedule"/> is <c>landing.view.bgtiming</c>, read as the same
/// date/code pairs a slot schedule uses. It is what selects a seasonal <see cref="HotelViewScene"/>.
/// </remarks>
public sealed record HotelViewCommon(
    string TextColor,
    string EtchingColor,
    string EtchingPosition,
    string LeftPaneWidth,
    string RightPaneWidth,
    string LayoutXml,
    string RoomCategory,
    bool RightPaneDimmerHidden,
    IReadOnlyList<HotelViewScheduleEntry> SceneSchedule
);
