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
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Users;

public class GetExtendedProfileByNameMessageHandler
    : IMessageHandler<GetExtendedProfileByNameMessage>
{
    private readonly IGrainFactory _grainFactory;

    public GetExtendedProfileByNameMessageHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async ValueTask HandleAsync(
        GetExtendedProfileByNameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        IPlayerDirectoryGrain directoryGrain = _grainFactory.GetPlayerDirectoryGrain();
        PlayerId? playerId = await directoryGrain
            .GetPlayerIdAsync(message.UserName, ct)
            .ConfigureAwait(false);

        if (playerId is null)
        {
            return;
        }

        PlayerExtendedProfileSnapshot snapshot = await _grainFactory
            .GetPlayerGrain(playerId.Value)
            .GetExtendedProfileSnapshotAsync(ct)
            .ConfigureAwait(false);

        // Guild membership lives in the group directory, not on the player grain, so the profile's
        // guild list is assembled here rather than shipped empty.
        List<GuildInfoSnapshot> guilds = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetMembershipsAsync(playerId.Value, ct)
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
                    IsOnline = snapshot.IsOnline,
                    Guilds = guilds,
                    LastAccessSinceInSeconds = snapshot.LastAccessSinceInSeconds,
                    OpenProfileWindow = snapshot.OpenProfileWindow,
                    IsHidden = snapshot.IsHidden,
                    AccountLevel = snapshot.AccountLevel,
                    IntegerField24 = snapshot.IntegerField24,
                    StarGemCount = snapshot.StarGemCount,
                    BooleanField26 = snapshot.BooleanField26,
                    BooleanField27 = snapshot.BooleanField27,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
