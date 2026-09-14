using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The hotel view, read out of the flat <c>landing.view.*</c> keys of external_variables.json.
/// </summary>
/// <remarks>
/// This is a projection, not a store. The file is the state -- it is what the client downloads at
/// boot -- and the same shape goes back on a save, so what the page shows and what the file holds
/// cannot describe different hotels.
/// <para>
/// <paramref name="ModifiedUtc"/> and <paramref name="TextsModifiedUtc"/> travel back with the save
/// as the caller's expected values. Two files are involved because a promo's words live in
/// external_flash_texts and its wiring lives in external_variables; each is guarded separately.
/// </para>
/// <para>
/// <paramref name="Placeholders"/> resolves the <c>${…}</c> tokens the stored URLs are written with.
/// A background is configured as <c>${image.library.url}reception/background_left.png</c>, and
/// without the value of <c>image.library.url</c> a page can show the operator the string they typed
/// but never the picture they picked.
/// </para>
/// </remarks>
public sealed record HotelViewConfig(
    bool Available,
    DateTime? ModifiedUtc,
    DateTime? TextsModifiedUtc,
    HotelViewCommon Common,
    IReadOnlyList<HotelViewSlot> Slots,
    IReadOnlyList<HotelViewCampaign> Campaigns,
    IReadOnlyList<HotelViewScene> Scenes,
    IReadOnlyList<HotelViewExtra> Extras,
    IReadOnlyDictionary<string, string> Placeholders,
    HotelViewVocabulary Vocabulary
);
