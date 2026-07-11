using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Navigator;

namespace Turbo.PacketHandlers.Navigator;

public class GetPopularRoomTagsMessageHandler(INavigatorProvider navigatorProvider)
    : IMessageHandler<GetPopularRoomTagsMessage>
{
    private const int Limit = 10;

    public async ValueTask HandleAsync(
        GetPopularRoomTagsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<string> tags = await navigatorProvider
            .GetPopularTagsAsync(Limit, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new PopularRoomTagsResultMessageComposer { Tags = tags }, ct)
            .ConfigureAwait(false);
    }
}
