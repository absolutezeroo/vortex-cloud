using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.FriendList;

public class SetRelationshipStatusMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetRelationshipStatusMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SetRelationshipStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);

        await grain
            .SetRelationshipStatusAsync(message.FriendId, message.RelationType, ct)
            .ConfigureAwait(false);
    }
}
