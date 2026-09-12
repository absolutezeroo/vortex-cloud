using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.WebApi.Services;

/// <summary>
/// An appart as the website shows one — the gallery row and the room page draw from the same record,
/// because habbo.com's two screens show the same fields with different emphasis.
/// </summary>
/// <remarks>
/// <para>
/// <paramref name="Tags"/> is the room's two tag columns with the empty ones dropped: the client
/// writes exactly two, and a list is what both pages render.
/// </para>
/// <para>
/// <paramref name="DoorOpen"/> is what habbo.com's `room.html` branches on
/// (<c>room.doorMode != 'open'</c>): a room behind a doorbell or a password gets a different screen
/// — "L'accès à l'appart est restreint." — rather than the full page with an enter button. It is a
/// BOOLEAN and not the door mode itself on purpose: whether the door wants a password or a doorbell
/// is the client's business, and telling a web page which one would be telling a stranger how to
/// prepare for it.
/// </para>
/// </remarks>
public sealed record RoomSummary(
    int Id,
    string Name,
    string Description,
    string OwnerName,
    int UsersNow,
    int MaximumVisitors,
    int Score,
    IReadOnlyList<string> Tags,
    bool DoorOpen
);

/// <summary>One page of the appart gallery.</summary>
public sealed record RoomPage(int Page, int PageSize, int Total, IReadOnlyList<RoomSummary> Items);

/// <summary>
/// The appart reads the website makes. The navigator's own data reaches the CLIENT over the game
/// socket; these are the same rooms read straight from the database for a page a visitor can open
/// without signing in.
/// </summary>
public interface IWebApiRoomService
{
    /// <summary>The gallery: the busiest visible apparts first.</summary>
    Task<RoomPage> GetRoomsAsync(int page, int pageSize, CancellationToken ct);

    /// <summary>One appart, or <c>null</c> when it does not exist or is not visible.</summary>
    Task<RoomSummary?> GetRoomAsync(int roomId, CancellationToken ct);
}
