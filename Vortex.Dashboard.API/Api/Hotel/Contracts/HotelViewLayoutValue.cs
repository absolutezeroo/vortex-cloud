namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One entry of a widget's <c>layout</c> string, which positions its illustration and text column.
/// </summary>
/// <remarks>
/// <c>&lt;key&gt;,&lt;value&gt;</c> pairs joined with <c>;</c>. The keys are the nine
/// <c>GenericWidget.configureLayout</c> accepts; anything else falls through its switch in silence.
/// </remarks>
public sealed record HotelViewLayoutValue(string Key, string Value);
