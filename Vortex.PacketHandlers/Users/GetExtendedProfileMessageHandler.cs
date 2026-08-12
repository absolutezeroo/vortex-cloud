using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Users;

public class GetExtendedProfileMessageHandler : IMessageHandler<GetExtendedProfileMessage>
{
    private readonly IGrainFactory _grainFactory;

    public GetExtendedProfileMessageHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async ValueTask HandleAsync(
        GetExtendedProfileMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        PlayerId targetUserId = message.UserId;

        if (targetUserId <= 0)
        {
            return;
        }

        PlayerExtendedProfileSnapshot snapshot = await _grainFactory
            .GetPlayerGrain(targetUserId)
            .GetExtendedProfileSnapshotAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        // Guild membership lives in the group directory, not on the player grain, so the profile's
        // guild list is assembled here rather than shipped empty.
        List<GuildInfoSnapshot> guilds = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetMembershipsAsync(targetUserId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new ExtendedProfileMessageComposer
                {
                    UserId = (int)snapshot.UserId,
                    UserName = snapshot.UserName,
                    Figure = snapshot.Figure,
                    Motto = snapshot.Motto,
                    CreationDate = snapshot.CreationDate,
                    AchievementScore = snapshot.AchievementScore,
                    FriendCount = snapshot.FriendCount,
                    IsFriend = snapshot.IsFriend,
                    IsFriendRequestSent = snapshot.IsFriendRequestSent,
                    // Hidden wins over online: the client's three icons come off this one byte,
                    // and a hidden player who also reads as online would show the wrong one.
                    OnlineStatus =
                        snapshot.IsHidden ? OnlineStatusCodes.Hidden
                        : snapshot.IsOnline ? OnlineStatusCodes.Online
                        : OnlineStatusCodes.Offline,
                    Guilds = guilds,
                    LastAccessSinceInSeconds = snapshot.LastAccessSinceInSeconds,
                    OpenProfileWindow = snapshot.OpenProfileWindow,
                    IsHidden = snapshot.IsHidden,
                    AccountLevel = snapshot.AccountLevel,
                    IntegerField24 = snapshot.IntegerField24,
                    StarGemCount = snapshot.StarGemCount,
                    BooleanField26 = snapshot.BooleanField26,
                    BooleanField27 = snapshot.BooleanField27,
                    // The two counts are real. The rarity breakdown and the rank are still zero:
                    // `player_badges` stores neither a rarity tier nor an owner count, so there is
                    // nothing to compute them from. They stay on the wire because the client reads
                    // all four unconditionally - omitting them truncates the packet.
                    TotalBadges = snapshot.TotalBadges,
                    AchievementLevel = snapshot.AchievementLevel,
                    BadgeRarityCounts = [],
                    TotalBadgesRank = 0,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
