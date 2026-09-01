using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Wired;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    private const int WiredPermanentVariableActionSet = 0;
    private const int WiredPermanentVariableActionCreate = 1;
    private const int WiredPermanentVariableActionDelete = 2;

    public async Task<WiredPermanentVariablesSnapshot> GetPermanentVariablesForEntityAsync(
        WiredVariableTargetType targetType,
        int targetId,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        List<WiredPermanentVariableEntity> rows = await dbCtx
            .WiredPermanentVariables.AsNoTracking()
            .Where(v => v.TargetType == targetType && v.TargetId == targetId)
            .ToListAsync(ct);

        List<(string VariableId, int Value)> variables = rows.Select(v => (v.VariableId, v.Value))
            .ToList();

        string entityName = string.Empty;
        string entityFigure = string.Empty;
        int? ownerId = null;
        string? ownerName = null;
        string? ownerFigure = null;

        if (targetType == WiredVariableTargetType.User)
        {
            PlayerSummarySnapshot? summary = await TryGetPlayerSummaryAsync(targetId, ct);

            entityName = summary?.Name ?? string.Empty;
            entityFigure = summary?.Figure ?? string.Empty;
        }
        else if (targetType == WiredVariableTargetType.Furni)
        {
            RoomFloorItemSnapshot? item = await GetFloorItemSnapshotByIdAsync(
                new RoomObjectId(targetId),
                ct
            );

            if (item is not null)
            {
                ownerId = item.OwnerId.Value;
                ownerName = item.OwnerName;
                ownerFigure = (await TryGetPlayerSummaryAsync(item.OwnerId.Value, ct))?.Figure;
            }
        }

        return new WiredPermanentVariablesSnapshot
        {
            EntityType = targetType,
            EntityId = targetId,
            EntityName = entityName,
            EntityFigure = entityFigure,
            OwnerId = ownerId,
            OwnerName = ownerName,
            OwnerFigure = ownerFigure,
            Variables = variables,
        };
    }

    public async Task<bool> SetPermanentVariableAsync(
        ActionContext ctx,
        WiredVariableTargetType targetType,
        int targetId,
        string variableId,
        int value,
        int action,
        CancellationToken ct
    )
    {
        // Whoever may decorate the room may edit its wired variables -- the same bar the contract
        // store applies. Without this the packet was open to any connected player: both client
        // surfaces that send it (the variable manager and the inspection tab) live inside the wired
        // menu, which the client only opens for someone holding rights, and that precondition was
        // stated nowhere on the server.
        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return false;
        }

        // ponytail: a Furni target is checked to be in this room -- the id is otherwise any object
        // id in the hotel. A User target cannot be scoped the same way: wired_permanent_variables
        // carries no room column by design (a permanent variable follows the player between rooms),
        // so the rights check above is the whole guard for that case. Narrowing it further needs a
        // policy decision on what a user variable is scoped to, which the specs do not answer.
        if (
            targetType == WiredVariableTargetType.Furni
            && await GetFloorItemSnapshotByIdAsync(new RoomObjectId(targetId), ct) is null
        )
        {
            return false;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        WiredPermanentVariableEntity? existing =
            await dbCtx.WiredPermanentVariables.FirstOrDefaultAsync(
                v =>
                    v.TargetType == targetType
                    && v.TargetId == targetId
                    && v.VariableId == variableId,
                ct
            );

        if (action == WiredPermanentVariableActionDelete)
        {
            if (existing is null)
            {
                return false;
            }

            dbCtx.WiredPermanentVariables.Remove(existing);

            await dbCtx.SaveChangesAsync(ct);

            return true;
        }

        if (existing is not null)
        {
            existing.Value = value;
        }
        else
        {
            dbCtx.WiredPermanentVariables.Add(
                new WiredPermanentVariableEntity
                {
                    TargetType = targetType,
                    TargetId = targetId,
                    VariableId = variableId,
                    Value = value,
                }
            );
        }

        await dbCtx.SaveChangesAsync(ct);

        return true;
    }

    public async Task<WiredVariableOwnersPageSnapshot> GetVariableOwnersPageAsync(
        string variableId,
        int page,
        int pageSize,
        int userTypeFilter,
        int sortTypeFilter,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        IQueryable<WiredPermanentVariableEntity> query = dbCtx
            .WiredPermanentVariables.AsNoTracking()
            .Where(v => v.VariableId == variableId);

        // Best-effort: userTypeFilter is unverified against a real client capture. When it maps
        // to a real WiredVariableTargetType value, filter to it; otherwise return every target
        // type holding this variable. See plan residual-risk notes.
        if (Enum.IsDefined(typeof(WiredVariableTargetType), userTypeFilter))
        {
            WiredVariableTargetType filterType = (WiredVariableTargetType)userTypeFilter;

            query = query.Where(v => v.TargetType == filterType);
        }

        int totalEntries = await query.CountAsync(ct);

        // Best-effort: sortTypeFilter meaning is unverified. 1 = ascending by value, anything
        // else = descending by value (a leaderboard-style default).
        query =
            sortTypeFilter == 1
                ? query.OrderBy(v => v.Value)
                : query.OrderByDescending(v => v.Value);

        int safePageSize = pageSize > 0 ? pageSize : 25;
        int safePage = page > 0 ? page : 1;

        List<WiredPermanentVariableEntity> rows = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(ct);

        List<WiredVariableOwnerEntry> elements = new(rows.Count);

        foreach (WiredPermanentVariableEntity row in rows)
        {
            string entityName = string.Empty;

            if (row.TargetType == WiredVariableTargetType.User)
            {
                entityName =
                    (await TryGetPlayerSummaryAsync(row.TargetId, ct))?.Name ?? string.Empty;
            }

            elements.Add(
                new WiredVariableOwnerEntry
                {
                    EntityId = row.TargetId,
                    EntityName = entityName,
                    Value = row.Value,
                }
            );
        }

        return new WiredVariableOwnersPageSnapshot
        {
            VariableId = variableId,
            TotalEntries = totalEntries,
            CurrentPage = safePage,
            Amount = elements.Count,
            Elements = elements,
            UserTypeFilter = userTypeFilter,
            SortTypeFilter = sortTypeFilter,
        };
    }

    private async Task<PlayerSummarySnapshot?> TryGetPlayerSummaryAsync(
        int playerId,
        CancellationToken ct
    )
    {
        try
        {
            return await _grainFactory.GetPlayerGrain(new PlayerId(playerId)).GetSummaryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to resolve player summary for player {PlayerId} while building a wired permanent-variable snapshot in room {RoomId}",
                playerId,
                _state.RoomId
            );

            return null;
        }
    }
}
