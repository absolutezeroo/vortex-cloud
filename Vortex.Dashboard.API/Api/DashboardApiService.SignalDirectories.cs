using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Habbicons;
using Vortex.Database.Entities.Navigator;
using Vortex.Database.Entities.Pets;
using Vortex.Database.Entities.Players;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The directories behind the filter pickers.
/// </summary>
/// <remarks>
/// <para>
/// Every one of these is a value an operator was previously expected to type into a filter from
/// memory: a guild id, a Habbicon id, a badge code, a catalogue offer, a navigator category, a pet
/// species. A typo in any of them produces a filter that is valid, saveable, and silently never
/// matches — the one failure this whole subsystem exists to make impossible, reappearing at the
/// last step because the value was free text.
/// </para>
/// <para>
/// They share the shape the picker already speaks: <c>{ id, name, … }</c> with <c>q</c>,
/// <c>limit</c>, <c>offset</c> and <c>hasMore</c>, so no new component is needed — only a new kind.
/// </para>
/// </remarks>
internal sealed partial class DashboardApiService
{
    /// <summary>Guilds, by name or id.</summary>
    public Task<object> GroupsDirectoryAsync(NameValueCollection query, CancellationToken ct) =>
        PickerPageAsync(
            query,
            db => db.Groups.AsNoTracking(),
            (groups, term, id) =>
                groups.Where(g => g.Name.Contains(term) || (id != null && g.Id == id)),
            groups => groups.OrderBy(g => g.Name),
            g => new PickerRow(g.Id, g.Name, g.Description),
            ct
        );

    /// <summary>Habbicons, by code or id.</summary>
    public Task<object> HabbiconsDirectoryAsync(NameValueCollection query, CancellationToken ct) =>
        PickerPageAsync(
            query,
            db => db.Habbicons.AsNoTracking().Where(h => h.Enabled),
            (icons, term, id) =>
                icons.Where(h => h.Code.Contains(term) || (id != null && h.Id == id)),
            icons => icons.OrderBy(h => h.SortOrder).ThenBy(h => h.Code),
            h => new PickerRow(h.Id, h.Code, null),
            ct
        );

    /// <summary>Habbicon collections. The value a filter stores is the code, not the id.</summary>
    public Task<object> HabbiconCollectionsDirectoryAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        PickerPageAsync(
            query,
            db => db.HabbiconCollections.AsNoTracking().Where(c => c.Enabled),
            (collections, term, id) =>
                collections.Where(c => c.Code.Contains(term) || (id != null && c.Id == id)),
            collections => collections.OrderBy(c => c.SortOrder).ThenBy(c => c.Code),
            // The signal carries the code, so that is what the picker must hand back as the value.
            c => new PickerRow(c.Id, c.Code, null) { Value = c.Code },
            ct
        );

    /// <summary>Catalogue offers, by localization id.</summary>
    public Task<object> CatalogOffersDirectoryAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        PickerPageAsync(
            query,
            db => db.CatalogOffers.AsNoTracking(),
            (offers, term, id) =>
                offers.Where(o => o.LocalizationId.Contains(term) || (id != null && o.Id == id)),
            offers => offers.OrderBy(o => o.LocalizationId),
            o => new PickerRow(o.Id, o.LocalizationId, null),
            ct
        );

    /// <summary>Navigator flat categories.</summary>
    public Task<object> NavigatorCategoriesDirectoryAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        PickerPageAsync(
            query,
            db => db.NavigatorFlatCategories.AsNoTracking().Where(c => c.Visible),
            (categories, term, id) =>
                categories.Where(c => c.Name.Contains(term) || (id != null && c.Id == id)),
            categories => categories.OrderBy(c => c.Name),
            c => new PickerRow(c.Id, c.Name, null),
            ct
        );

    /// <summary>
    /// Badge codes actually in circulation.
    /// </summary>
    /// <remarks>
    /// There is no badge catalogue table — a badge exists because somebody was granted one — so this
    /// reads the distinct codes players hold. It is therefore a list of badges that have been
    /// awarded at least once, which is exactly the set a task can sensibly ask for: a code nobody
    /// has ever held would be a filter nobody can satisfy.
    /// </remarks>
    public Task<object> BadgesDirectoryAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = ParseLimit(query["limit"], 50, 200);
                int offset = int.TryParse(query["offset"], out int parsed)
                    ? Math.Max(0, parsed)
                    : 0;

                IQueryable<string> codes = db
                    .PlayerBadges.AsNoTracking()
                    .Select(b => b.BadgeCode)
                    .Distinct();

                if (term.Length > 0)
                {
                    codes = codes.Where(c => c.Contains(term));
                }

                int total = await codes.CountAsync(ct).ConfigureAwait(false);

                List<string> page = await codes
                    .OrderBy(c => c)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var items = page.Select(code => new
                    {
                        id = code,
                        value = code,
                        name = code,
                        description = (string?)null,
                    })
                    .ToList();

                return new
                {
                    count = items.Count,
                    total,
                    offset,
                    hasMore = offset + items.Count < total,
                    items,
                };
            },
            ct
        );

    /// <summary>
    /// Pet species, from the palettes that define them.
    /// </summary>
    /// <remarks>
    /// A species is not a row of its own: it is the distinct <c>PetType</c> across the palettes the
    /// hotel ships, which is what a task filtering on "a pet of species N" compares against.
    /// </remarks>
    public Task<object> PetSpeciesDirectoryAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                string term = (query["q"] ?? string.Empty).Trim();

                List<int> types = await db
                    .PetPalettes.AsNoTracking()
                    .Select(p => p.PetType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var items = types
                    .Where(t =>
                        term.Length == 0 || t.ToString().Contains(term, StringComparison.Ordinal)
                    )
                    .Select(t => new
                    {
                        id = t,
                        value = t.ToString(),
                        name = $"Species {t}",
                        description = (string?)null,
                    })
                    .ToList();

                return new
                {
                    count = items.Count,
                    total = items.Count,
                    offset = 0,
                    hasMore = false,
                    items,
                };
            },
            ct
        );

    /// <summary>What a picker row is, whatever table it came from.</summary>
    private sealed record PickerRow(int Id, string Name, string? Description)
    {
        /// <summary>What a filter stores. The id, unless the signal carries something else.</summary>
        public string? Value { get; init; }
    }

    /// <summary>
    /// One paged, searchable picker query, so six directories are six expressions rather than six
    /// copies of the same twenty lines.
    /// </summary>
    private Task<object> PickerPageAsync<TEntity>(
        NameValueCollection query,
        Func<Database.Context.VortexDbContext, IQueryable<TEntity>> source,
        Func<IQueryable<TEntity>, string, int?, IQueryable<TEntity>> search,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order,
        Func<TEntity, PickerRow> project,
        CancellationToken ct
    ) =>
        QueryAsync<object>(
            async db =>
            {
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = ParseLimit(query["limit"], 50, 200);
                int offset = int.TryParse(query["offset"], out int parsed)
                    ? Math.Max(0, parsed)
                    : 0;

                IQueryable<TEntity> rows = source(db);

                if (term.Length > 0)
                {
                    rows = search(rows, term, int.TryParse(term, out int id) ? id : null);
                }

                int total = await rows.CountAsync(ct).ConfigureAwait(false);

                List<TEntity> page = await order(rows)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var items = page.Select(project)
                    .Select(r => new
                    {
                        id = r.Id,
                        value = r.Value ?? r.Id.ToString(),
                        name = r.Name,
                        description = r.Description,
                    })
                    .ToList();

                return new
                {
                    count = items.Count,
                    total,
                    offset,
                    hasMore = offset + items.Count < total,
                    items,
                };
            },
            ct
        );
}
