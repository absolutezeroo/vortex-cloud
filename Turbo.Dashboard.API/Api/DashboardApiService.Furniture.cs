using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Furniture;

namespace Turbo.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
    /// <summary>
    /// Paginated furniture-definition admin listing with every field the edit form needs. Kept
    /// separate from <see cref="FurnitureDefinitionsAsync"/> (the compact picker search used by
    /// PickerModal/OperationsPage) since that one is called on every keystroke and only needs a
    /// handful of display fields.
    /// </summary>
    public Task<object> FurnitureDefinitionAdminListAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<object>(
            async db =>
            {
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = ParseLimit(query["limit"], 40, 200);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                IQueryable<FurnitureDefinitionEntity> definitions =
                    db.FurnitureDefinitions.AsNoTracking();

                if (term.Length > 0)
                {
                    if (int.TryParse(term, out int id))
                    {
                        definitions = definitions.Where(f =>
                            f.Name.Contains(term) || f.Id == id || f.SpriteId == id
                        );
                    }
                    else
                    {
                        definitions = definitions.Where(f => f.Name.Contains(term));
                    }
                }

                int total = await definitions.CountAsync(ct).ConfigureAwait(false);

                var rows = await definitions
                    .OrderBy(f => f.Name)
                    .Skip(offset)
                    .Take(limit)
                    .Select(f => new
                    {
                        f.Id,
                        f.SpriteId,
                        f.Name,
                        productType = (int)f.ProductType,
                        productTypeLabel = f.ProductType.ToString(),
                        furniCategory = (int)f.FurniCategory,
                        furniCategoryLabel = f.FurniCategory.ToString(),
                        f.Logic,
                        f.TotalStates,
                        f.Width,
                        f.Length,
                        f.StackHeight,
                        f.CanStack,
                        f.CanWalk,
                        f.CanSit,
                        f.CanLay,
                        f.CanRecycle,
                        f.CanTrade,
                        f.CanGroup,
                        f.CanSell,
                        usagePolicy = (int)f.UsagePolicy,
                        usagePolicyLabel = f.UsagePolicy.ToString(),
                        f.ExtraData,
                        stuffDataType = (int)f.StuffDataType,
                        stuffDataTypeLabel = f.StuffDataType.ToString(),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var items = rows.Select(f => new
                    {
                        f.Id,
                        f.SpriteId,
                        f.Name,
                        f.productType,
                        f.productTypeLabel,
                        f.furniCategory,
                        f.furniCategoryLabel,
                        f.Logic,
                        f.TotalStates,
                        f.Width,
                        f.Length,
                        f.StackHeight,
                        f.CanStack,
                        f.CanWalk,
                        f.CanSit,
                        f.CanLay,
                        f.CanRecycle,
                        f.CanTrade,
                        f.CanGroup,
                        f.CanSell,
                        f.usagePolicy,
                        f.usagePolicyLabel,
                        f.ExtraData,
                        f.stuffDataType,
                        f.stuffDataTypeLabel,
                        iconUrl = BuildFurniIconUrl(f.Name),
                    })
                    .ToList();

                return new
                {
                    page,
                    limit,
                    offset,
                    total,
                    count = items.Count,
                    items,
                };
            },
            ct
        );
}
