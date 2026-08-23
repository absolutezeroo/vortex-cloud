using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Primitives.Action;
using Vortex.Primitives.Groups.Enums;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<RoomControllerType> GetControllerLevelAsync(
        ActionContext ctx,
        CancellationToken ct
    ) => SecurityModule.GetControllerLevelAsync(ctx);

    public async Task UpdateAvatarFavouriteGroupAsync(
        PlayerId playerId,
        int groupId,
        string groupName,
        CancellationToken ct
    )
    {
        if (!_state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId))
        {
            return;
        }

        if (
            !_state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            || avatar is not IRoomPlayer player
        )
        {
            return;
        }

        if (!player.SetFavouriteGroup(groupId, groupName))
        {
            return;
        }

        // The client keys this off the avatar's room index (its object id), not the player id.
        await SendComposerToRoomAsync(
                new FavoriteMembershipUpdateMessageComposer
                {
                    RoomIndex = objectId.Value,
                    GroupId = player.GroupId,
                    Status =
                        player.GroupId > 0
                            ? GroupMembershipStatus.Member
                            : GroupMembershipStatus.NotMember,
                    GroupName = player.GroupName,
                }
            )
            .ConfigureAwait(true);
    }

    public async Task RefreshGroupMembershipAsync(
        IReadOnlyList<int> affectedPlayerIds,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            // Read the link back from the room rather than trusting the cached snapshot: a guild
            // that was just dissolved has already cleared rooms.group_id, and continuing to serve
            // the stale roster would keep granting build rights to its former members.
            var link = await dbCtx
                .Rooms.AsNoTracking()
                .Where(r => r.Id == _state.RoomId.Value)
                .Select(r => new
                {
                    r.GroupEntityId,
                    GroupName = r.GroupEntity!.Name,
                    GroupBadge = r.GroupEntity!.Badge,
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(true);

            if (link is null)
            {
                return;
            }

            if (_state.RoomSnapshot.GroupId != link.GroupEntityId)
            {
                _state.RoomSnapshot = _state.RoomSnapshot with
                {
                    GroupId = link.GroupEntityId,
                    GroupName = link.GroupEntityId is null ? null : link.GroupName,
                    GroupBadge = link.GroupEntityId is null ? null : link.GroupBadge,
                };
            }

            await HydrateGroupMembershipAsync(dbCtx, link.GroupEntityId, ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to refresh guild membership for room {RoomId}.",
                _state.RoomId
            );
            return;
        }

        // Only players standing in the room have a controller level worth re-pushing. An empty
        // affected list means the guild's settings changed, which moves everyone's level at once.
        List<PlayerId> targets =
            affectedPlayerIds.Count == 0
                ? [.. _state.AvatarsByPlayerId.Keys]
                :
                [
                    .. affectedPlayerIds
                        .Select(id => (PlayerId)id)
                        .Where(_state.AvatarsByPlayerId.ContainsKey),
                ];

        foreach (PlayerId target in targets)
        {
            try
            {
                await SecurityModule
                    .RefreshControllerLevelForPlayerAsync(target, ct)
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to push guild controller level to player {PlayerId} in room {RoomId}.",
                    target,
                    _state.RoomId
                );
            }
        }
    }
}
