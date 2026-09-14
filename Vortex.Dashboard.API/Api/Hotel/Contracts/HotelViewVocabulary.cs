using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// Everything the client will accept, sent to the page so its dropdowns are the client's own lists.
/// </summary>
/// <remarks>
/// These are closed sets read out of the AS3 source, not suggestions. A widget type outside
/// <see cref="WidgetTypes"/>, a layer outside <see cref="BackgroundLayers"/> or a motion outside
/// <see cref="MotionTypes"/> is not rejected by anything -- the client simply renders nothing and
/// logs nothing, which is the failure mode this page exists to remove.
/// <para>
/// It travels with every read rather than being copied into the front end, so the lists cannot drift
/// from what the server validates against.
/// </para>
/// </remarks>
public sealed record HotelViewVocabulary(
    IReadOnlyList<string> WidgetTypes,
    IReadOnlyList<HotelViewElementType> ElementTypes,
    IReadOnlyList<string> LayoutKeys,
    IReadOnlyList<string> BackgroundLayers,
    IReadOnlyList<HotelViewMotionType> MotionTypes,
    IReadOnlyList<HotelViewSlotShape> SlotShapes,
    int MaxObjects
);
