namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// A <c>landing.view.*</c> key this page models no field for, kept so that saving cannot drop it.
/// </summary>
/// <remarks>
/// Two different things land here and the page says which:
/// <list type="bullet">
/// <item><paramref name="Read"/> true -- a key the client genuinely reads, belonging to a fixed
/// widget this page does not lay out (the catalogue promo, the community goal, the quest strip).
/// Editable as a raw value.</item>
/// <item><paramref name="Read"/> false -- a key <b>no</b> code path in the target client looks at.
/// Setting it does nothing at all. <c>landing_view_promo_slots</c>, <c>landing_view.url</c> and
/// <c>landing_view.use_web</c> are in this bucket: they survive in every dump that gets copied
/// around, and every hotel that configures them is configuring nothing.</item>
/// </list>
/// </remarks>
public sealed record HotelViewExtra(string Key, string Value, bool Read);
