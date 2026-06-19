using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

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
            return;

        var count = await _grainFactory
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
