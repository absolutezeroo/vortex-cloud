namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One of the nine named background layers of the hotel view.</summary>
/// <remarks>
/// The names are windows in the layout XML, not free labels: the client walks its own fixed list and
/// looks each one up by name, so a layer it does not know about can never be drawn.
/// <para>
/// <c>Visible</c> false writes <c>.visible = "false"</c>, which the client tests as a literal string;
/// any other value, including the absence of the key, shows the layer.
/// </para>
/// </remarks>
public sealed record HotelViewBackground(string Layer, string Uri, bool Visible);
