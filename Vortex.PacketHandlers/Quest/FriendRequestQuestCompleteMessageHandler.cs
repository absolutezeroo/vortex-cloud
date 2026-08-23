using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests;
using Vortex.Protocol.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

/// <summary>
/// The client reports its "ask for a friend" quest step: <c>HabboFriendList.askForAFriend()</c> sends
/// this immediately after the friend request itself, with an empty body. It advances on the asking,
/// not on the answer — a quest that should only count accepted friends uses
/// <see cref="QuestTypes.FriendListSize"/> instead.
/// </summary>
public class FriendRequestQuestCompleteMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<FriendRequestQuestCompleteMessage>
{
    public async ValueTask HandleAsync(
        FriendRequestQuestCompleteMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerQuestGrain(ctx.PlayerId)
            .ProgressAsync(QuestTypes.FriendRequestSent, 1, ct)
            .ConfigureAwait(false);
    }
}
