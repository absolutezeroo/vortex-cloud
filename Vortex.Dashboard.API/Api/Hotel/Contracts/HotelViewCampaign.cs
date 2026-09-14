using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One campaign: the widget a schedule switches a slot to, under a short code.
/// </summary>
/// <remarks>
/// Three keys share the code -- <c>landing.view.&lt;code&gt;.widget</c>, <c>.conf</c> and
/// <c>.layout</c> -- and the code is also what a schedule entry names and what the server answers
/// with. A campaign no schedule mentions is dead weight rather than an error, so
/// <paramref name="UsedBy"/> says which slots reach it; an empty list is the page's way of showing
/// the ones nobody can see.
/// </remarks>
public sealed record HotelViewCampaign(
    string Code,
    string Widget,
    IReadOnlyList<HotelViewElement> Elements,
    IReadOnlyList<HotelViewLayoutValue> Layout,
    IReadOnlyList<int> UsedBy
);
