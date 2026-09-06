using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Navigator.Enums;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The navigator's own configuration: the tabs the client asks for, the blocks inside them, and the
/// two category tables.
/// <para>
/// This is the one dashboard surface where "no rows" is the interesting state: an unseeded hotel
/// answers every navigator request with an empty left pane, and a tab with no quick links renders
/// blank however healthy the room list is. Both are reported explicitly rather than shown as an
/// empty table the operator has to interpret.
/// </para>
/// </summary>
internal sealed partial class DashboardApiService
{
    public Task<object> NavigatorConfigAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var contexts = await db
                    .NavigatorTopLevelContexts.AsNoTracking()
                    .OrderBy(c => c.OrderNum)
                    .ThenBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.SearchCode,
                        c.Visible,
                        queryType = (int)c.QueryType,
                        queryTypeLabel = c.QueryType.ToString(),
                        c.OrderNum,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var quickLinks = await db
                    .NavigatorQuickLinks.AsNoTracking()
                    .OrderBy(q => q.TopLevelContextEntityId)
                    .ThenBy(q => q.OrderNum)
                    .Select(q => new
                    {
                        q.Id,
                        contextId = q.TopLevelContextEntityId,
                        q.SearchCode,
                        q.Filter,
                        q.Localization,
                        queryType = (int)q.QueryType,
                        queryTypeLabel = q.QueryType.ToString(),
                        q.OrderNum,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var flatCategories = await db
                    .NavigatorFlatCategories.AsNoTracking()
                    .OrderBy(c => c.OrderNum)
                    .ThenBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Visible,
                        c.Automatic,
                        c.AutomaticCategory,
                        c.GlobalCategory,
                        c.StaffOnly,
                        c.MinRank,
                        c.OrderNum,
                        roomCount = db.Rooms.Count(r =>
                            r.NavigatorCategoryEntityId == c.Id && r.DeletedAt == null
                        ),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                DateTime now = DateTime.UtcNow;

                var eventCategories = await db
                    .NavigatorEventCategories.AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Visible,
                        activeAdCount = db.RoomAdvertisements.Count(a =>
                            a.CategoryId == c.Id && a.ExpiresAt > now
                        ),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, int> linkCountByContext = quickLinks
                    .GroupBy(q => q.contextId)
                    .ToDictionary(g => g.Key, g => g.Count());

                // The four codes the client itself asks for. A hotel missing one of these has a tab
                // that answers nothing at all, which is invisible from the table alone.
                List<string> missingTabs = NavigatorSearchCodes
                    .TopLevelViews.Where(view =>
                        !contexts.Exists(c =>
                            string.Equals(c.SearchCode, view, StringComparison.Ordinal)
                        )
                    )
                    .OrderBy(view => view, StringComparer.Ordinal)
                    .ToList();

                var emptyTabs = contexts
                    .Where(c => linkCountByContext.GetValueOrDefault(c.Id) == 0)
                    .Select(c => new { c.Id, c.SearchCode })
                    .ToList();

                var items = contexts
                    .Select(c => new
                    {
                        c.Id,
                        c.SearchCode,
                        c.Visible,
                        c.queryType,
                        c.queryTypeLabel,
                        c.OrderNum,
                        knownCode = NavigatorSearchCodes.QueryTypeBySearchCode.ContainsKey(
                            c.SearchCode
                        ),
                        quickLinks = quickLinks
                            .Where(q => q.contextId == c.Id)
                            .Select(q => new
                            {
                                q.Id,
                                q.SearchCode,
                                q.Filter,
                                q.Localization,
                                q.queryType,
                                q.queryTypeLabel,
                                q.OrderNum,
                                knownCode = NavigatorSearchCodes.QueryTypeBySearchCode.ContainsKey(
                                    q.SearchCode
                                ),
                            })
                            .ToList(),
                    })
                    .ToList();

                return new
                {
                    health = new
                    {
                        contextCount = contexts.Count,
                        quickLinkCount = quickLinks.Count,
                        flatCategoryCount = flatCategories.Count,
                        eventCategoryCount = eventCategories.Count,
                        missingTabs,
                        emptyTabs,
                        seeded = contexts.Count > 0 && quickLinks.Count > 0,
                    },
                    contexts = items,
                    flatCategories,
                    eventCategories,
                    searchCodes = NavigatorSearchCodes
                        .QueryTypeBySearchCode.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => new
                        {
                            code = pair.Key,
                            queryType = (int)pair.Value,
                            queryTypeLabel = pair.Value.ToString(),
                            topLevel = NavigatorSearchCodes.TopLevelViews.Contains(pair.Key),
                        })
                        .ToList(),
                    queryTypes = Enum.GetValues<NavigatorQueryType>()
                        .Select(value => new { value = (int)value, label = value.ToString() })
                        .ToList(),
                };
            },
            ct
        );
}
