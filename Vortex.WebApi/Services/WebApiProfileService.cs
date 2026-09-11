using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Players.Enums;

namespace Vortex.WebApi.Services;

/// <summary>
/// Reads a player's public profile straight out of the database — the badges they own, the friends
/// they keep, the rooms they own and the groups they belong to.
/// </summary>
/// <remarks>
/// Read-only and anonymous, so every query is <c>AsNoTracking</c> and none of them touches a grain:
/// a profile is a page a signed-out visitor can open, and waking a player's grain to render one
/// would make a crawler's traffic into cluster load. The cost is that <c>Online</c> comes from the
/// persisted <c>PlayerStatus</c> column rather than from the live session, so it is as fresh as the
/// last write of that column — right for a web page, and never a reason to hold a grain open.
/// </remarks>
public sealed class WebApiProfileService(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<WebApiProfileService> logger
) : IWebApiProfileService
{
    /// <summary>
    /// Badge slots are 1-5 on the avatar; <c>SlotId</c> is null for a badge that is merely owned.
    /// </summary>
    private const int WornSlotMinimum = 1;

    private readonly IDbContextFactory<VortexDbContext> _db = dbCtxFactory;
    private readonly ILogger<WebApiProfileService> _logger = logger;

    public async Task<ProfileUser?> FindUserByNameAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        PlayerEntity? player = await db
            .Players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == name && p.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (player is null)
        {
            return null;
        }

        return await BuildUserAsync(db, player, ct).ConfigureAwait(false);
    }

    public async Task<PlayerProfile?> GetProfileAsync(int playerId, CancellationToken ct)
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        PlayerEntity? player = await db
            .Players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId && p.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (player is null)
        {
            _logger.LogDebug("Profile requested for unknown player {PlayerId}", playerId);

            return null;
        }

        ProfileUser user = await BuildUserAsync(db, player, ct).ConfigureAwait(false);

        List<ProfileBadge> badges = await db
            .PlayerBadges.AsNoTracking()
            .Where(b => b.PlayerEntityId == playerId)
            .OrderBy(b => b.SlotId == null)
            .ThenBy(b => b.SlotId)
            .ThenBy(b => b.BadgeCode)
            .Select(b => new ProfileBadge(b.SlotId ?? 0, b.BadgeCode))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // The friend row is directed — each side of a friendship has its own — so reading the rows
        // this player owns is the whole list, with no second query for the mirror side.
        List<ProfileFriend> friends = await db
            .MessengerFriends.AsNoTracking()
            .Where(f => f.PlayerEntityId == playerId && f.FriendPlayerEntity.DeletedAt == null)
            .OrderBy(f => f.FriendPlayerEntity.Name)
            .Select(f => new ProfileFriend(
                f.FriendPlayerEntityId.ToString(CultureInfo.InvariantCulture),
                f.FriendPlayerEntity.Name,
                f.FriendPlayerEntity.Figure,
                f.FriendPlayerEntity.Motto ?? string.Empty,
                f.FriendPlayerEntity.PlayerStatus == PlayerStatusType.Online
            ))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<ProfileRoom> rooms = await db
            .Rooms.AsNoTracking()
            .Where(r => r.PlayerEntityId == playerId && r.DeletedAt == null)
            .OrderByDescending(r => r.UsersNow)
            .ThenBy(r => r.Name)
            .Select(r => new ProfileRoom(
                r.Id,
                r.Name,
                r.Description ?? string.Empty,
                r.UsersNow,
                r.PlayersMax
            ))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<ProfileGroup> groups = await db
            .GroupMembers.AsNoTracking()
            .Where(m => m.PlayerEntityId == playerId && m.GroupEntity.DeletedAt == null)
            .OrderBy(m => m.GroupEntity.Name)
            .Select(m => new ProfileGroup(
                m.GroupEntityId,
                m.GroupEntity.Name,
                m.GroupEntity.Description ?? string.Empty,
                m.GroupEntity.RoomEntityId,
                m.GroupEntity.Badge,
                m.GroupEntity.ColorOne,
                m.GroupEntity.ColorTwo,
                m.Rank == GroupMemberRank.Admin || m.GroupEntity.OwnerPlayerEntityId == playerId
            ))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PlayerProfile(user, badges, friends, rooms, groups);
    }

    /// <summary>
    /// The header, shared by the name lookup and the full read. The worn badges are a separate query
    /// rather than a filter over the full badge list: the lookup route answers with the header alone
    /// and has no reason to read a player's entire badge collection.
    /// </summary>
    private static async Task<ProfileUser> BuildUserAsync(
        VortexDbContext db,
        PlayerEntity player,
        CancellationToken ct
    )
    {
        List<ProfileBadge> worn = await db
            .PlayerBadges.AsNoTracking()
            .Where(b => b.PlayerEntityId == player.Id && b.SlotId >= WornSlotMinimum)
            .OrderBy(b => b.SlotId)
            .Select(b => new ProfileBadge(b.SlotId ?? 0, b.BadgeCode))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new ProfileUser(
            player.Id.ToString(CultureInfo.InvariantCulture),
            player.Name,
            player.Figure,
            player.Motto ?? string.Empty,
            player.CreatedAt,
            // No column backs this yet — see IWebApiProfileService.ProfileUser.
            ProfileVisible: true,
            player.PlayerStatus == PlayerStatusType.Online,
            player.AchievementScore,
            player.RespectReceived,
            worn
        );
    }
}
