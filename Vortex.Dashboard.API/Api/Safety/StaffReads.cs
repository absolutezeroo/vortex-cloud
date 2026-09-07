using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Safety.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Api.Safety;

/// <summary>
/// Who can do what: the roles, the capabilities each one grants, the accounts holding them, and the
/// sanction preset ladder the mod tool offers.
/// <para>
/// Two cross-checks are done here rather than left to the operator, because both are silent
/// failures: a role granting a capability string that no longer exists in
/// <see cref="Capabilities.All"/> grants nothing at all, and a declared capability that no role
/// grants is a feature nobody can reach.
/// </para>
/// </summary>
internal sealed class StaffReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    DashboardAssetUrls assetUrls
) : DashboardReads(dbContextFactory)
{
    private readonly DashboardAssetUrls _assetUrls = assetUrls;

    /// <summary>
    /// Account search for the role-assignment form. Roles hang off the <b>account</b>, not the
    /// player, so the ordinary player picker cannot drive this: it hands back a player id, and two
    /// players can share one account. Matches on email or on any of the account's player names.
    /// </summary>
    public Task<StaffAccountSearch> StaffAccountSearchAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<StaffAccountSearch>(
            async db =>
            {
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = QueryValues.Limit(query["limit"], 20, 50);

                IQueryable<Database.Entities.Players.PlayerAccountEntity> accounts = db
                    .PlayerAccounts.AsNoTracking()
                    .Where(a => a.DeletedAt == null);

                if (term.Length > 0)
                {
                    accounts = accounts.Where(a =>
                        a.Email.Contains(term)
                        || db.Players.Any(p =>
                            p.PlayerAccountEntityId == a.Id && p.Name.Contains(term)
                        )
                    );
                }

                List<StaffAccountMatch> rows = await accounts
                    .OrderBy(a => a.Email)
                    .Take(limit)
                    .Select(a => new StaffAccountMatch(
                        a.Id,
                        a.Email,
                        db.Players.Where(p => p.PlayerAccountEntityId == a.Id)
                            .Select(p => p.Name)
                            .ToList(),
                        db.PlayerAccountRoles.Where(r => r.PlayerAccountEntityId == a.Id)
                            .Select(r => r.RoleEntityId)
                            .ToList()
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new StaffAccountSearch(rows.Count, rows);
            },
            ct
        );

    public Task<StaffOverview> StaffAsync(CancellationToken ct) =>
        QueryAsync<StaffOverview>(
            async db =>
            {
                var roles = await db
                    .Roles.AsNoTracking()
                    .OrderBy(r => r.Id)
                    .Select(r => new
                    {
                        r.Id,
                        r.Key,
                        r.Name,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var permissions = await db
                    .RolePermissions.AsNoTracking()
                    .Select(p => new { p.RoleEntityId, p.CapabilityKey })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var holders = await db
                    .PlayerAccountRoles.AsNoTracking()
                    .Select(a => new { a.RoleEntityId, a.PlayerAccountEntityId })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, List<string>> capabilitiesByRole = permissions
                    .GroupBy(p => p.RoleEntityId)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                            g.Select(p => p.CapabilityKey)
                                .OrderBy(k => k, StringComparer.Ordinal)
                                .ToList()
                    );

                Dictionary<int, int> holderCountByRole = holders
                    .GroupBy(h => h.RoleEntityId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(h => h.PlayerAccountEntityId).Distinct().Count()
                    );

                HashSet<string> declared = new(Capabilities.All, StringComparer.Ordinal);

                List<StaffRole> roleItems = roles
                    .Select(r =>
                    {
                        List<string> caps = capabilitiesByRole.GetValueOrDefault(r.Id, []);

                        return new StaffRole(
                            r.Id,
                            r.Key,
                            r.Name,
                            caps.Count,
                            caps,
                            // A key that is not in Capabilities.All grants nothing: the
                            // authorization check compares against the declared set.
                            caps.Where(c => !declared.Contains(c)).ToList(),
                            caps.Contains(Capabilities.Wildcard),
                            holderCountByRole.GetValueOrDefault(r.Id)
                        );
                    })
                    .ToList();

                HashSet<string> granted = new(
                    permissions.Select(p => p.CapabilityKey),
                    StringComparer.Ordinal
                );

                // The wildcard role covers everything, so "ungranted" only means something when no
                // role holds it — otherwise every capability would list as ungranted on a hotel that
                // only uses the owner role.
                bool wildcardExists = granted.Contains(Capabilities.Wildcard);

                List<string> ungranted = wildcardExists
                    ? []
                    : Capabilities
                        .All.Where(c =>
                            !granted.Contains(c)
                            && !string.Equals(c, Capabilities.Wildcard, StringComparison.Ordinal)
                        )
                        .OrderBy(c => c, StringComparer.Ordinal)
                        .ToList();

                List<int> staffAccountIds = holders
                    .Select(h => h.PlayerAccountEntityId)
                    .Distinct()
                    .ToList();

                var accountRows = await db
                    .PlayerAccounts.AsNoTracking()
                    .Where(a => staffAccountIds.Contains(a.Id))
                    .Select(a => new
                    {
                        a.Id,
                        a.Email,
                        a.CreatedAt,
                        players = db
                            .Players.Where(p => p.PlayerAccountEntityId == a.Id)
                            .Select(p => new
                            {
                                p.Id,
                                p.Name,
                                p.Figure,
                            })
                            .ToList(),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // An operator recognises a staff member by their habbo, not by the email they signed
                // up with, so the account carries its avatars.
                Dictionary<int, string> roleNameById = roles.ToDictionary(r => r.Id, r => r.Name);

                List<StaffMember> staff = accountRows
                    .Select(a => new StaffMember(
                        a.Id,
                        a.Email,
                        a.CreatedAt,
                        a.players.ConvertAll(p => p.Name),
                        a.players.ConvertAll(p => new StaffPlayer(
                            p.Id,
                            p.Name,
                            _assetUrls.AvatarImage(p.Figure)
                        )),
                        holders
                            .Where(h => h.PlayerAccountEntityId == a.Id)
                            .Select(h =>
                                roleNameById.GetValueOrDefault(h.RoleEntityId, $"#{h.RoleEntityId}")
                            )
                            .OrderBy(name => name, StringComparer.Ordinal)
                            .ToList(),
                        // The ids, not just the names: the roster revokes a role in place now, and
                        // an assignment is addressed by (accountId, roleId). Resolving a name back
                        // to an id in the browser breaks the moment two roles are renamed alike.
                        holders
                            .Where(h => h.PlayerAccountEntityId == a.Id)
                            .Select(h => h.RoleEntityId)
                            .OrderBy(
                                id => roleNameById.GetValueOrDefault(id, $"#{id}"),
                                StringComparer.Ordinal
                            )
                            .ToList()
                    ))
                    .OrderBy(a => a.Email, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<SanctionPresetRow> presets = await db
                    .SanctionPresets.AsNoTracking()
                    .OrderBy(p => p.Kind)
                    .ThenBy(p => p.PresetIndex)
                    .Select(p => new SanctionPresetRow(
                        p.Id,
                        p.Kind.ToString(),
                        p.PresetIndex,
                        p.Name,
                        p.DurationSeconds,
                        p.Message,
                        p.DurationSeconds == null
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // A ban row survives its own expiry, so "active" is the expiry date, not the row's
                // presence.
                DateTime now = DateTime.UtcNow;
                int activeBans = await db
                    .AccountBans.AsNoTracking()
                    .CountAsync(b => b.DeletedAt == null && b.DateExpires > now, ct)
                    .ConfigureAwait(false);

                return new StaffOverview(
                    new StaffTotals(
                        roles.Count,
                        staff.Count,
                        Capabilities.All.Count,
                        granted.Count,
                        ungranted.Count,
                        presets.Count,
                        activeBans
                    ),
                    roleItems,
                    staff,
                    presets,
                    ungranted,
                    wildcardExists,
                    // Every declared capability, grouped by its namespace, so the role editor offers
                    // the real set instead of a free-text box that can store a key granting nothing.
                    Capabilities
                        .All.Where(c =>
                            !string.Equals(c, Capabilities.Wildcard, StringComparison.Ordinal)
                        )
                        .OrderBy(c => c, StringComparer.Ordinal)
                        .GroupBy(c => c.Split('.')[0])
                        .Select(g => new CapabilityGroup(g.Key, g.ToList()))
                        .ToList(),
                    Capabilities.Wildcard,
                    Enum.GetValues<SanctionPresetKind>()
                        .Select(k => new SanctionPresetKindOption((int)k, k.ToString()))
                        .ToList()
                );
            },
            ct
        );
}
