namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// "From this moment, show this campaign" -- one pair of a landing-view schedule string.
/// </summary>
/// <remarks>
/// The client does not resolve these itself: it sends the whole schedule to the server
/// (<c>GetCurrentTimingCode</c>) and renders whatever code comes back. Our handler answers with the
/// latest entry whose date has passed, so the pairs are read in ascending date order and an entry
/// with an empty <paramref name="Code"/> is how a campaign is switched off.
/// <para>
/// <paramref name="StartsAt"/> keeps the client's own <c>yyyy-MM-dd HH:mm</c> shape and its local
/// meaning -- it is compared against the server's local clock, not UTC.
/// </para>
/// </remarks>
public sealed record HotelViewScheduleEntry(string StartsAt, string Code);
