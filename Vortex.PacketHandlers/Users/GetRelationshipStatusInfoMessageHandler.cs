using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.PacketHandlers.Users;

public class GetRelationshipStatusInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetRelationshipStatusInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetRelationshipStatusInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // Get the target user's relationship summary (their relationship decorators)
        IMessengerGrain targetGrain = _grainFactory.GetMessengerGrain(message.UserId);
        List<RelationshipStatusEntrySnapshot> relations = await targetGrain
            .GetRelationshipStatusSummaryAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new RelationshipStatusInfoEventMessageComposer
                {
                    UserId = message.UserId,
                    Relations = relations,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
