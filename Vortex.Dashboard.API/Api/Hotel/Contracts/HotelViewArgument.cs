namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One positional argument of a landing-view element, named after what the client does with it.
/// </summary>
/// <remarks>
/// The client reads these as an untyped <c>String[]</c> split on commas, so nothing on the wire says
/// that the second field of <c>catalogbutton</c> is a catalogue page name. That knowledge only exists
/// in the AS3 source, and repeating it here is the whole point of this page: an operator composing
/// <c>caption,landing.view.jan26cf.header</c> by hand has no way to learn what the second field is.
/// </remarks>
/// <param name="Name">The identifier the front end looks up for a label and a description.</param>
/// <param name="Kind">
/// How to edit it: <c>text</c> (a key into external_flash_texts, edited as the string it resolves
/// to), <c>string</c>, <c>int</c>, <c>number</c>, <c>bool</c>, <c>url</c>, <c>image</c>,
/// <c>catalogPage</c>, <c>roomId</c>, <c>badge</c>.
/// </param>
/// <param name="Optional">Whether the client tolerates the argument being absent.</param>
public sealed record HotelViewArgument(string Name, string Kind, bool Optional);
