using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Permissions;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Permissions.Admin;

namespace Vortex.Authentication.Permissions;

/// <summary>
/// Writes for roles, role capabilities, role assignments and sanction presets. A plain singleton
/// opening a short-lived context per call, like the other admin services.
/// <para>
/// The one rule that is not obvious from the tables: every write that changes what an account may do
/// ends with <see cref="IPermissionService.InvalidateAccount"/> for each affected account. That
/// cache has a 60-second TTL, so a missed invalidation does not lose the grant — it delays it, which
/// is worse to diagnose than losing it.
/// </para>
/// </summary>
internal sealed class StaffAdminService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IPermissionService permissions
) : IStaffAdminService
{
    public async Task<StaffAdminResult> CreateRoleAsync(RoleSpec spec, CancellationToken ct)
    {
        if (Validate(spec) is { } error)
        {
            return StaffAdminResult.Fail(error);
        }

        string key = spec.Key.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (
            await db
                .Roles.AnyAsync(r => r.Key == key && r.DeletedAt == null, ct)
                .ConfigureAwait(false)
        )
        {
            return StaffAdminResult.Fail("role_key_taken");
        }

        RoleEntity entity = new() { Key = key, Name = spec.Name.Trim() };

        db.Roles.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return StaffAdminResult.Ok(entity.Id);
    }

    public async Task<StaffAdminResult> UpdateRoleAsync(
        int roleId,
        RoleSpec spec,
        CancellationToken ct
    )
    {
        if (Validate(spec) is { } error)
        {
            return StaffAdminResult.Fail(error);
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        RoleEntity? entity = await db
            .Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return StaffAdminResult.Fail("role_not_found");
        }

        entity.Key = spec.Key.Trim();
        entity.Name = spec.Name.Trim();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        // The key is part of the resolved PermissionSet, so holders must re-resolve.
        await InvalidateHoldersAsync(db, roleId, ct).ConfigureAwait(false);

        return StaffAdminResult.Ok(entity.Id);
    }

    public async Task<StaffAdminResult> DeleteRoleAsync(int roleId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        RoleEntity? entity = await db
            .Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return StaffAdminResult.Fail("role_not_found");
        }

        if (
            await db
                .PlayerAccountRoles.AnyAsync(a => a.RoleEntityId == roleId, ct)
                .ConfigureAwait(false)
        )
        {
            // Deleting a held role silently strips capabilities from whoever holds it, which is the
            // kind of change that should be made deliberately, one account at a time.
            return StaffAdminResult.Fail("role_still_assigned");
        }

        List<RolePermissionEntity> grants = await db
            .RolePermissions.Where(p => p.RoleEntityId == roleId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        db.RolePermissions.RemoveRange(grants);
        db.Roles.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return StaffAdminResult.Ok(roleId);
    }

    public async Task<StaffAdminResult> SetRoleCapabilitiesAsync(
        int roleId,
        RoleCapabilitiesSpec spec,
        CancellationToken ct
    )
    {
        HashSet<string> declared = new(
            Vortex.Primitives.Permissions.Capabilities.All,
            StringComparer.Ordinal
        );
        List<string> requested = spec
            .Capabilities.Select(c => c?.Trim() ?? string.Empty)
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (requested.Exists(c => !declared.Contains(c)))
        {
            // Storing a key the code does not declare grants nothing at all — refuse it here rather
            // than let it sit in the table looking granted.
            return StaffAdminResult.Fail("unknown_capability");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct).ConfigureAwait(false))
        {
            return StaffAdminResult.Fail("role_not_found");
        }

        List<RolePermissionEntity> existing = await db
            .RolePermissions.Where(p => p.RoleEntityId == roleId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        HashSet<string> keep = new(requested, StringComparer.Ordinal);

        db.RolePermissions.RemoveRange(existing.Where(p => !keep.Contains(p.CapabilityKey)));

        HashSet<string> already = new(
            existing.Select(p => p.CapabilityKey),
            StringComparer.Ordinal
        );

        foreach (string capability in requested.Where(c => !already.Contains(c)))
        {
            db.RolePermissions.Add(
                new RolePermissionEntity { RoleEntityId = roleId, CapabilityKey = capability }
            );
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await InvalidateHoldersAsync(db, roleId, ct).ConfigureAwait(false);

        return StaffAdminResult.Ok(roleId);
    }

    public async Task<StaffAdminResult> AssignRoleAsync(
        int accountId,
        int roleId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (!await db.PlayerAccounts.AnyAsync(a => a.Id == accountId, ct).ConfigureAwait(false))
        {
            return StaffAdminResult.Fail("account_not_found");
        }

        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct).ConfigureAwait(false))
        {
            return StaffAdminResult.Fail("role_not_found");
        }

        bool already = await db
            .PlayerAccountRoles.AnyAsync(
                a => a.PlayerAccountEntityId == accountId && a.RoleEntityId == roleId,
                ct
            )
            .ConfigureAwait(false);

        if (already)
        {
            return StaffAdminResult.Fail("role_already_assigned");
        }

        db.PlayerAccountRoles.Add(
            new PlayerAccountRoleEntity { PlayerAccountEntityId = accountId, RoleEntityId = roleId }
        );

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        permissions.InvalidateAccount(accountId);

        return StaffAdminResult.Ok(accountId);
    }

    public async Task<StaffAdminResult> UnassignRoleAsync(
        int accountId,
        int roleId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PlayerAccountRoleEntity? assignment = await db
            .PlayerAccountRoles.FirstOrDefaultAsync(
                a => a.PlayerAccountEntityId == accountId && a.RoleEntityId == roleId,
                ct
            )
            .ConfigureAwait(false);

        if (assignment is null)
        {
            return StaffAdminResult.Fail("assignment_not_found");
        }

        db.PlayerAccountRoles.Remove(assignment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        permissions.InvalidateAccount(accountId);

        return StaffAdminResult.Ok(accountId);
    }

    public async Task<StaffAdminResult> CreateSanctionPresetAsync(
        SanctionPresetSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return StaffAdminResult.Fail("name_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        bool taken = await db
            .SanctionPresets.AnyAsync(
                p =>
                    p.Kind == spec.Kind && p.PresetIndex == spec.PresetIndex && p.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (taken)
        {
            // The client picks a preset by (kind, index); two rows sharing one make the pick
            // ambiguous and the resolver keeps whichever comes first.
            return StaffAdminResult.Fail("preset_index_taken");
        }

        SanctionPresetEntity entity = new()
        {
            Kind = spec.Kind,
            PresetIndex = spec.PresetIndex,
            Name = spec.Name.Trim(),
            DurationSeconds = spec.DurationSeconds,
            Message = spec.Message,
        };

        db.SanctionPresets.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return StaffAdminResult.Ok(entity.Id);
    }

    public async Task<StaffAdminResult> UpdateSanctionPresetAsync(
        int presetId,
        SanctionPresetSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return StaffAdminResult.Fail("name_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        SanctionPresetEntity? entity = await db
            .SanctionPresets.FirstOrDefaultAsync(p => p.Id == presetId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return StaffAdminResult.Fail("preset_not_found");
        }

        entity.Kind = spec.Kind;
        entity.PresetIndex = spec.PresetIndex;
        entity.Name = spec.Name.Trim();
        entity.DurationSeconds = spec.DurationSeconds;
        entity.Message = spec.Message;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return StaffAdminResult.Ok(entity.Id);
    }

    public async Task<StaffAdminResult> DeleteSanctionPresetAsync(
        int presetId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        SanctionPresetEntity? entity = await db
            .SanctionPresets.FirstOrDefaultAsync(p => p.Id == presetId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return StaffAdminResult.Fail("preset_not_found");
        }

        db.SanctionPresets.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return StaffAdminResult.Ok(presetId);
    }

    private static string? Validate(RoleSpec spec) =>
        string.IsNullOrWhiteSpace(spec.Key) ? "role_key_required"
        : string.IsNullOrWhiteSpace(spec.Name) ? "role_name_required"
        : null;

    private async Task InvalidateHoldersAsync(VortexDbContext db, int roleId, CancellationToken ct)
    {
        List<int> accountIds = await db
            .PlayerAccountRoles.AsNoTracking()
            .Where(a => a.RoleEntityId == roleId)
            .Select(a => a.PlayerAccountEntityId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (int accountId in accountIds)
        {
            permissions.InvalidateAccount(accountId);
        }
    }
}
