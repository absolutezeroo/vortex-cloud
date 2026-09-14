using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One row of a <c>generic</c> widget's content column, as one entry of its <c>conf</c> string.
/// </summary>
/// <remarks>
/// On the wire this is <c>&lt;type&gt;,&lt;arg&gt;,&lt;arg&gt;</c> and the whole column is those
/// joined with <c>;</c>.
/// <para>
/// <paramref name="Text"/> is not part of that string. Where the element's first argument is a key
/// into external_flash_texts, this carries the string that key currently resolves to, so a promo can
/// be written in one place instead of composing a key here and going to find it in the texts file.
/// It is <see langword="null"/> for element types whose first argument is not a text key.
/// </para>
/// </remarks>
public sealed record HotelViewElement(string Type, IReadOnlyList<string> Args, string? Text);
