using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One complete set of scenery: nine background layers and up to twenty moving objects.
/// </summary>
/// <remarks>
/// <paramref name="Code"/> empty is the scenery the client shows before, and in the absence of, any
/// campaign. A non-empty code is a seasonal set, selected the same way a slot's campaign is: the
/// client sends <c>landing.view.bgtiming</c> to the server, and the code that comes back prefixes
/// both the layer keys and the object keys (<c>landing.view.&lt;code&gt;.background_left.uri</c>,
/// <c>landing.view.&lt;code&gt;.bgobject.1</c>).
/// <para>
/// The consequence worth knowing before building one: a seasonal set replaces nothing by default.
/// A layer the set does not name keeps whatever the default set gave it, because the client only
/// assigns the layers whose prefixed key is present.
/// </para>
/// </remarks>
public sealed record HotelViewScene(
    string Code,
    IReadOnlyList<HotelViewBackground> Layers,
    IReadOnlyList<HotelViewObject> Objects
);
