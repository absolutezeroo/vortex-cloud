using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>
/// The hotel view as the page now wants it -- the whole configuration, not a patch.
/// </summary>
/// <remarks>
/// Whole rather than field-by-field for one reason: removing something has to be expressible. A
/// deleted campaign, a cleared background, a slot emptied of its widget are all <b>keys that must
/// stop existing</b>, and a patch of the values that changed cannot say that. Sending the whole
/// configuration lets the server write exactly the keys it implies and delete every other
/// <c>landing.view.*</c> key, which is the same statement in one direction.
/// <para>
/// The two expected timestamps are what the page believed each file's write time to be. They are
/// separate because the words and the wiring are in separate files, and someone translating a
/// headline should not lose a save that only moved a slot.
/// </para>
/// </remarks>
public sealed record HotelViewSaveRequest(
    DateTime? ExpectedModifiedUtc,
    DateTime? ExpectedTextsModifiedUtc,
    HotelViewCommon Common,
    IReadOnlyList<HotelViewSlot> Slots,
    IReadOnlyList<HotelViewCampaign> Campaigns,
    IReadOnlyList<HotelViewScene> Scenes,
    IReadOnlyList<HotelViewExtra> Extras,
    string Reason
) : IReasonedRequest;
