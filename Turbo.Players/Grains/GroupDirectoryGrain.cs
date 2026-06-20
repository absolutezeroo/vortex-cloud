using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Groups;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Events;
using Turbo.Primitives.Groups.Enums;
using Turbo.Primitives.Groups.Grains;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Wallet;

namespace Turbo.Players.Grains;

/// <summary>
///     Singleton grain handling group operations not bound to one existing group: the creation wizard,
///     creating a group (charged via the wallet, with its 1:1 forum-settings row through
///     <see cref="GroupFactory" />), the player's memberships/favourite, and badge-editor data.
///     Per-group reads/mutations live on <see cref="GroupGrain" />.
/// </summary>
internal sealed class GroupDirectoryGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<GroupDirectoryGrain> logger
) : Grain, IGroupDirectoryGrain
{
    // Habbo's classic guild price. Kept here (not a hardcoded grain limit) as the single creation
    // cost constant; promote to IConfiguration if it needs to vary per hotel.
    private const int CreationCostInCredits = 10;

    // Catalog purchase-type label for guild creation (groups the spend in catalog metrics/audit).
    private const string CatalogPurchaseTypeGuild = "Guild";

    public async Task<GroupCreationInfoSnapshot> GetCreationInfoAsync(
        PlayerId player,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int playerId = player.Value;

        List<GroupRoomSnapshot> rooms = await dbCtx
            .Rooms.AsNoTracking()
            .Where(r => r.PlayerEntityId == playerId && r.DeletedAt == null)
            .OrderBy(r => r.Id)
            .Select(r => new GroupRoomSnapshot
            {
                RoomId = r.Id,
                RoomName = r.Name,
                // A room can host a guild only if it is not already a guild base.
                HasControllers = r.GroupEntityId == null
            })
            .ToListAsync(ct);

        return new GroupCreationInfoSnapshot
        {
            CostInCredits = CreationCostInCredits,
            Rooms = rooms,
            // Default starting badge the wizard pre-fills; the player edits it before confirming.
            BadgeParts = GuildBadgeLibrary.DefaultBadgeParts
        };
    }

    public async Task<int?> CreateGroupAsync(
        PlayerId owner,
        string name,
        string description,
        int primaryColorId,
        int secondaryColorId,
        int baseRoomId,
        IReadOnlyList<int> badgeParts,
        CancellationToken ct
    )
    {
        int ownerId = owner.Value;

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        RoomEntity? room = await dbCtx.Rooms.FirstOrDefaultAsync(
            r => r.Id == baseRoomId && r.DeletedAt == null,
            ct
        );

        // Must own the room, and the room must not already be a guild base.
        if (room is null || room.PlayerEntityId != ownerId || room.GroupEntityId != null)
        {
            return null;
        }

        PlayerEntity? ownerEntity = await dbCtx.Players.FindAsync([ownerId], ct);
        if (ownerEntity is null)
        {
            return null;
        }

        // Charge the creation cost through the wallet — this is the "purchase". The wallet writes
        // the economy ledger entry, emits CurrencyChangedEvent and pushes the new balance to the
        // client; we only proceed if the debit succeeds (insufficient funds → abort, no group).
        WalletDebitResult debit = await grainFactory
            .GetPlayerWalletGrain(ownerId)
            .TryDebitAsync(
                [
                    new WalletDebitRequest
                    {
                        CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                        Amount = CreationCostInCredits
                    }
                ],
                ct
            )
            .ConfigureAwait(false);

        if (!debit.Succeeded)
        {
            logger.LogInformation(
                "Player {OwnerId} could not afford guild creation ({Cost} credits)",
                ownerId,
                CreationCostInCredits
            );
            return null;
        }

        GroupEntity group = GroupFactory.Create(
            name,
            BuildBadgeCode(badgeParts),
            baseRoomId,
            ownerId,
            GroupType.Open,
            primaryColorId.ToString(),
            secondaryColorId.ToString(),
            string.IsNullOrEmpty(description) ? null : description
        );
        group.RoomEntity = room;
        group.OwnerPlayerEntity = ownerEntity;

        dbCtx.Groups.Add(group);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        // Link the room back to the group and enrol the owner as an admin member.
        room.GroupEntityId = group.Id;
        dbCtx.GroupMembers.Add(
            new GroupMemberEntity
            {
                GroupEntityId = group.Id,
                PlayerEntityId = ownerId,
                Rank = GroupMemberRank.Admin,
                GroupEntity = group,
                PlayerEntity = ownerEntity
            }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        // Hook the spend into the catalog purchase path, exactly like CatalogPurchaseGrain: track
        // the credit spend (Club payday) and raise CatalogPurchasedEvent so guild creation shows up
        // in catalog purchase metrics/audit alongside every other purchase.
        await grainFactory
            .GetPlayerGrain(owner)
            .TrackCreditSpendAsync(CreationCostInCredits, ct)
            .ConfigureAwait(false);

        await events
            .PublishAsync(
                new CatalogPurchasedEvent(
                    ownerId,
                    CatalogPurchaseTypeGuild,
                    0,
                    1,
                    CreationCostInCredits
                ),
                ct
            )
            .ConfigureAwait(false);

        await events
            .PublishAsync(
                new GroupCreatedEvent(
                    ownerId,
                    group.Id,
                    group.Name,
                    baseRoomId,
                    CreationCostInCredits
                ),
                ct
            )
            .ConfigureAwait(false);

        logger.LogInformation(
            "Player {OwnerId} created group {GroupId} on room {RoomId}",
            ownerId,
            group.Id,
            baseRoomId
        );

        return group.Id;
    }

    public async Task<List<GuildInfoSnapshot>> GetMembershipsAsync(
        PlayerId player,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int playerId = player.Value;

        int? favouriteGroupId = await dbCtx
            .Players.AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => p.FavouriteGroupId)
            .FirstOrDefaultAsync(ct);

        // Groups the player belongs to (membership table already includes the owner).
        List<int> groupIds = await dbCtx
            .GroupMembers.AsNoTracking()
            .Where(m => m.PlayerEntityId == playerId && m.DeletedAt == null)
            .Select(m => m.GroupEntityId)
            .ToListAsync(ct);

        if (groupIds.Count == 0)
        {
            return [];
        }

        List<GroupEntity> groups = await dbCtx
            .Groups.AsNoTracking()
            .Include(g => g.ForumSettings)
            .Where(g => groupIds.Contains(g.Id) && g.DeletedAt == null)
            .ToListAsync(ct);

        return groups
            .Select(g => new GuildInfoSnapshot
            {
                GroupId = g.Id,
                GroupName = g.Name,
                BadgeCode = g.Badge,
                // ColorOne/ColorTwo store the palette colour id; resolve to hex for the client.
                PrimaryColor = GuildBadgeLibrary.ResolveColorHex(g.ColorOne),
                SecondaryColor = GuildBadgeLibrary.ResolveColorHex(g.ColorTwo),
                Favourite = favouriteGroupId == g.Id,
                OwnerId = g.OwnerPlayerEntityId,
                HasForum = g.ForumSettings?.Enabled ?? false
            })
            .ToList();
    }

    public async Task SetFavouriteAsync(
        PlayerId player,
        int groupId,
        bool favourite,
        CancellationToken ct
    )
    {
        int playerId = player.Value;

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        // Only a member may favourite a group; clearing is unconditional.
        if (favourite)
        {
            bool isMember = await dbCtx.GroupMembers.AnyAsync(
                m =>
                    m.GroupEntityId == groupId
                    && m.PlayerEntityId == playerId
                    && m.DeletedAt == null,
                ct
            );
            if (!isMember)
            {
                return;
            }
        }

        int? newValue = favourite ? groupId : null;

        await dbCtx
            .Players.Where(p => p.Id == playerId)
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.FavouriteGroupId, newValue), ct)
            .ConfigureAwait(false);

        await events
            .PublishAsync(new GroupFavouriteChangedEvent(playerId, newValue), ct)
            .ConfigureAwait(false);
    }

    public Task<GroupEditorDataSnapshot> GetEditorDataAsync(CancellationToken ct)
    {
        // Badge designer palette: base shapes, overlay symbols and colour swatches the client
        // renders in the editor. Sourced from GuildBadgeLibrary (the part/colour ids are stable
        // and are what the persisted badge code references).
        return Task.FromResult(
            new GroupEditorDataSnapshot
            {
                BaseParts = GuildBadgeLibrary.BaseParts,
                LayerParts = GuildBadgeLibrary.LayerParts,
                BadgeColors = GuildBadgeLibrary.BadgeColors,
                PrimaryColors = GuildBadgeLibrary.PrimaryColors,
                SecondaryColors = GuildBadgeLibrary.SecondaryColors
            }
        );
    }

    public async Task<List<GroupBadgeSnapshot>> GetBadgesAsync(
        PlayerId player,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int playerId = player.Value;

        List<int> groupIds = await dbCtx
            .GroupMembers.AsNoTracking()
            .Where(m => m.PlayerEntityId == playerId && m.DeletedAt == null)
            .Select(m => m.GroupEntityId)
            .ToListAsync(ct);

        if (groupIds.Count == 0)
        {
            return [];
        }

        return await dbCtx
            .Groups.AsNoTracking()
            .Where(g => groupIds.Contains(g.Id) && g.DeletedAt == null)
            .Select(g => new GroupBadgeSnapshot { GroupId = g.Id, BadgeCode = g.Badge })
            .ToListAsync(ct);
    }

    public async Task<ForumsListPageSnapshot> GetForumsListAsync(
        PlayerId player,
        int listCode,
        int startIndex,
        int amount,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int take = amount is <= 0 or > 50 ? 20 : amount;
        int skip = Math.Max(startIndex, 0);

        IQueryable<GroupEntity> enabledForums = dbCtx
            .Groups.AsNoTracking()
            .Where(g => g.DeletedAt == null && g.ForumSettings!.Enabled);

        int totalAmount = await enabledForums.CountAsync(ct);

        var groups = await enabledForums
            .OrderByDescending(g => g.Id)
            .Skip(skip)
            .Take(take)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.Badge
            })
            .ToListAsync(ct);

        if (groups.Count == 0)
        {
            return new ForumsListPageSnapshot
            {
                ListCode = listCode,
                TotalAmount = totalAmount,
                StartIndex = startIndex,
                Forums = []
            };
        }

        List<int> groupIds = groups.Select(g => g.Id).ToList();

        Dictionary<int, int> threadCounts = await dbCtx
            .GroupForumThreads.AsNoTracking()
            .Where(t =>
                groupIds.Contains(t.GroupEntityId)
                && t.DeletedAt == null
                && t.State != GroupForumThreadState.Hidden
            )
            .GroupBy(t => t.GroupEntityId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        Dictionary<int, int> messageCounts = await dbCtx
            .GroupForumPosts.AsNoTracking()
            .Where(p =>
                groupIds.Contains(p.GroupEntityId)
                && p.DeletedAt == null
                && p.State == GroupForumPostState.Visible
            )
            .GroupBy(p => p.GroupEntityId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        List<ForumSnapshot> forums = groups
            .Select(g => new ForumSnapshot
            {
                GroupId = g.Id,
                Name = g.Name,
                Description = g.Description ?? string.Empty,
                Icon = g.Badge,
                TotalThreads = threadCounts.GetValueOrDefault(g.Id, 0),
                LeaderboardScore = 0,
                TotalMessages = messageCounts.GetValueOrDefault(g.Id, 0),
                UnreadMessages = 0,
                LastMessageId = 0,
                LastMessageAuthorId = 0,
                LastMessageAuthorName = string.Empty,
                LastMessageTimeAsSecondsAgo = 0,
                // ForumsList only serializes the base fields; extended values are unused here.
                ReadPermissions = 0,
                PostMessagePermissions = 0,
                PostThreadPermissions = 0,
                ModeratePermissions = 0,
                ReadPermissionError = string.Empty,
                PostMessagePermissionError = string.Empty,
                PostThreadPermissionError = string.Empty,
                ModeratePermissionError = string.Empty,
                ReportPermissionError = string.Empty,
                CanChangeSettings = false,
                IsStaff = false
            })
            .ToList();

        return new ForumsListPageSnapshot
        {
            ListCode = listCode,
            TotalAmount = totalAmount,
            StartIndex = startIndex,
            Forums = forums
        };
    }

    public Task<int> GetUnreadForumsCountAsync(PlayerId player, CancellationToken ct)
    {
        // Per-user read markers are not tracked yet (UpdateForumReadMarker is accepted but not
        // persisted), so there is no unread state to count. Returns 0; revisit with read tracking.
        return Task.FromResult(0);
    }

    /// <summary>
    ///     Assembles a deterministic guild badge code from the flattened part triples
    ///     (partId, colorId, position) the client submits. Exact glyph rendering depends on the
    ///     badge-parts config (the GuildEditorData feature); this preserves the player's selection.
    /// </summary>
    internal static string BuildBadgeCode(IReadOnlyList<int> parts)
    {
        if (parts.Count == 0)
        {
            return "b00000";
        }

        string code = string.Empty;
        for (int i = 0; i + 2 < parts.Count; i += 3)
        {
            code += $"b{parts[i]:D2}{parts[i + 1]:D2}{parts[i + 2]}";
        }

        return code.Length == 0 ? "b00000" : code;
    }
}
