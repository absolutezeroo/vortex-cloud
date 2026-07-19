using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
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
            .GetExtendedProfileSnapshotAsync(ct)
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
                    Guilds = snapshot.Guilds,
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
