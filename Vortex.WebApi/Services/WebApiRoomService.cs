using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.WebApi.Services;

/// <summary>
/// The website's appart reads, straight out of the database and anonymous.
/// </summary>
/// <remarks>
/// <para>
/// A room whose door is <see cref="RoomDoorModeType.Invisible"/> is excluded everywhere here,
/// including from the direct read: the whole point of that setting is that the room does not appear
/// to someone who was not told about it, and a web page that answers for it is a way around the
/// door the client enforces.
/// </para>
/// <para>
/// <c>UsersNow</c> is the persisted column, not a live count — the same trade the profile makes for
/// <c>Online</c>. It is what the navigator persists, so the gallery's ordering is as fresh as the
/// last time a room wrote it, and no grain is woken to render a page.
/// </para>
/// </remarks>
public sealed class WebApiRoomService(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<WebApiRoomService> logger
) : IWebApiRoomService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 50;

    private readonly IDbContextFactory<VortexDbContext> _db = dbCtxFactory;
    private readonly ILogger<WebApiRoomService> _logger = logger;

    public async Task<RoomPage> GetRoomsAsync(int page, int pageSize, CancellationToken ct)
    {
        int wantedPage = page < 1 ? 1 : page;
        int wantedSize = pageSize switch
        {
            < 1 => DefaultPageSize,
            > MaximumPageSize => MaximumPageSize,
            _ => pageSize,
        };

        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        IQueryable<RoomEntity> visible = db
            .Rooms.AsNoTracking()
            .Where(r => r.DeletedAt == null && r.DoorMode != RoomDoorModeType.Invisible);

        int total = await visible.CountAsync(ct).ConfigureAwait(false);

        List<RoomRow> rows = await Project(
                visible
                    .OrderByDescending(r => r.UsersNow)
                    .ThenByDescending(r => r.Score)
                    .ThenBy(r => r.Id)
                    .Skip((wantedPage - 1) * wantedSize)
                    .Take(wantedSize)
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new RoomPage(wantedPage, wantedSize, total, rows.ConvertAll(ToSummary));
    }

    public async Task<RoomSummary?> GetRoomAsync(int roomId, CancellationToken ct)
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        RoomRow? room = await Project(
                db.Rooms.AsNoTracking()
                    .Where(r =>
                        r.Id == roomId
                        && r.DeletedAt == null
                        && r.DoorMode != RoomDoorModeType.Invisible
                    )
            )
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (room is null)
        {
            _logger.LogDebug("Room {RoomId} requested and not visible", roomId);

            return null;
        }

        return ToSummary(room);
    }

    /// <summary>
    /// The columns both reads need. A projection rather than the entity: reading the row itself
    /// materialises every paint and every moderation setting for the seven fields a web page shows.
    /// The two tag columns come back raw and are folded into a list afterwards — dropping empties
    /// inside the query is a client-side operation EF would have to be asked to translate.
    /// </summary>
    private sealed record RoomRow(
        int Id,
        string Name,
        string? Description,
        string OwnerName,
        int UsersNow,
        int MaximumVisitors,
        int Score,
        string? Tag1,
        string? Tag2,
        bool DoorOpen
    );

    private static IQueryable<RoomRow> Project(IQueryable<RoomEntity> rooms) =>
        rooms.Select(r => new RoomRow(
            r.Id,
            r.Name,
            r.Description,
            r.PlayerEntity.Name,
            r.UsersNow,
            r.PlayersMax,
            r.Score,
            r.Tag1,
            r.Tag2,
            // Collapsed to a boolean in the QUERY, so which kind of door it is never leaves the
            // database. Invisible is already excluded everywhere above, so what reaches here is Open
            // against Locked-or-Password.
            r.DoorMode == RoomDoorModeType.Open
        ));

    private static RoomSummary ToSummary(RoomRow row)
    {
        List<string> tags = [];

        if (!string.IsNullOrWhiteSpace(row.Tag1))
        {
            tags.Add(row.Tag1);
        }

        if (!string.IsNullOrWhiteSpace(row.Tag2))
        {
            tags.Add(row.Tag2);
        }

        return new RoomSummary(
            row.Id,
            row.Name,
            row.Description ?? string.Empty,
            row.OwnerName,
            row.UsersNow,
            row.MaximumVisitors,
            row.Score,
            tags,
            row.DoorOpen
        );
    }
}
