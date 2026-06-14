using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

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
            return;

        // Get the target user's relationship summary (their relationship decorators)
        var targetGrain = _grainFactory.GetMessengerGrain(message.UserId);
        var relations = await targetGrain
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
