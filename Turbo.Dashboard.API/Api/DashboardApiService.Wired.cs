using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;

namespace Turbo.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
    /// <summary>Read-only overview of placed wired furniture: how many wired pieces exist, which
    /// rooms lean on wired the most, and the trigger/condition/action/addon mix. There is no
    /// dedicated wired-placement table — every wired-logic furniture definition's <c>Logic</c> key
    /// is prefixed <c>wf_trg_</c>/<c>wf_cnd_</c>/<c>wf_act_</c>/<c>wf_var_</c>/<c>wf_slc_</c>/
    /// <c>wf_xtra_</c> (see <c>Turbo.Rooms/Object/Logic/Furniture/Floor/Wired/**</c>'s
    /// <c>[RoomObjectLogic("wf_...")]</c> attributes), so placed wired furniture is just placed
    /// <see cref="Turbo.Database.Entities.Furniture.FurnitureEntity"/> rows whose definition's Logic
    /// starts with <c>wf_</c>. Per-piece trigger/condition/action configuration lives in each row's
    /// <c>ExtraData</c> JSON blob, which is intentionally not parsed here (opaque, logic-specific
    /// shape) — this endpoint reports placement/category volume, not wired-script contents.</summary>
    public Task<object> WiredStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                List<(int RoomEntityId, string Logic)> placed = await (
                    from f in db.Furnitures.AsNoTracking()
                    join d in db.FurnitureDefinitions.AsNoTracking()
                        on f.FurnitureDefinitionEntityId equals d.Id
                    where f.RoomEntityId != null && d.Logic.StartsWith("wf_")
                    select new ValueTuple<int, string>(f.RoomEntityId!.Value, d.Logic)
                )
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int totalWiredPlaced = placed.Count;
                int roomsWithWired = placed.Select(p => p.RoomEntityId).Distinct().Count();

                var byCategory = placed
                    .GroupBy(p => CategorizeWiredLogic(p.Logic))
                    .Select(g => new { category = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ToList();

                var byLogic = placed
                    .GroupBy(p => p.Logic)
                    .Select(g => new { logic = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .Take(20)
                    .ToList();

                var topRoomGroups = placed
                    .GroupBy(p => p.RoomEntityId)
                    .Select(g => new { roomId = g.Key, wiredCount = g.Count() })
                    .OrderByDescending(g => g.wiredCount)
                    .Take(10)
                    .ToList();

                List<int> roomIds = topRoomGroups.Select(g => g.roomId).ToList();
                Dictionary<int, string> roomNames = await LoadRoomNamesAsync(db, roomIds, ct)
                    .ConfigureAwait(false);

                var topRooms = topRoomGroups
                    .Select(g => new
                    {
                        g.roomId,
                        roomName = roomNames.GetValueOrDefault(g.roomId, $"room #{g.roomId}"),
                        g.wiredCount,
                    })
                    .ToList();

                return new
                {
                    totals = new { totalWiredPlaced, roomsWithWired },
                    byCategory,
                    byLogic,
                    topRooms,
                };
            },
            ct
        );

    private static string CategorizeWiredLogic(string logic) =>
        logic switch
        {
            _ when logic.StartsWith("wf_trg_") => "trigger",
            _ when logic.StartsWith("wf_cnd_") => "condition",
            _ when logic.StartsWith("wf_act_") => "action",
            _ when logic.StartsWith("wf_var_") => "variable",
            _ when logic.StartsWith("wf_slc_") => "selector",
            _ when logic.StartsWith("wf_xtra_") => "addon",
            _ => "other",
        };
}
