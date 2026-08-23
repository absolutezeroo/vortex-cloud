using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Users;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.PacketHandlers.Users;

public class GetMemberGuildItemCountMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetMemberGuildItemCountMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetMemberGuildItemCountMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        int count = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .GetMemberFurniCountAsync(message.UserId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new GuildMemberFurniCountInHQMessageComposer
                {
                    UserId = message.UserId,
                    FurniCount = count,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
