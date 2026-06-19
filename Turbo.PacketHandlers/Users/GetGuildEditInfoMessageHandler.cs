using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

public class GetGuildEditInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuildEditInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuildEditInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
            return;

        var info = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .GetEditInfoAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (info is null)
            return;

        await ctx.SendComposerAsync(new GuildEditInfoMessageComposer { Info = info }, ct)
            .ConfigureAwait(false);
    }
}
