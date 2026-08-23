using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Server.Grains;
using Vortex.Protocol.Messages.Incoming.Users;
using Vortex.Protocol.Messages.Outgoing.Notifications;

namespace Vortex.PacketHandlers.Users;

public class GetMOTDMessageHandler(IGrainFactory grainFactory) : IMessageHandler<GetMOTDMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetMOTDMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<string> lines = await _grainFactory
            .GetServerConfigGrain()
            .GetMotdAsync()
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new MOTDNotificationEventMessageComposer { Messages = [.. lines] },
                ct
            )
            .ConfigureAwait(false);
    }
}
