using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Nft;
using Vortex.Primitives.Messages.Outgoing.Nft;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Nft;

/// <summary>
/// The avatars the player may wear, for the editor's NFT tab.
/// </summary>
/// <remarks>
/// Asked for every time the editor is built, before the player has clicked anything, so an empty
/// list is the ordinary answer and is sent as such — the tab simply shows nothing.
/// </remarks>
public class GetUserNftWardrobeMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetUserNftWardrobeMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetUserNftWardrobeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<NftAvatarSnapshot> avatars = await _grainFactory
            .GetPlayerNftWardrobeGrain(new PlayerId(ctx.PlayerId))
            .GetWardrobeAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new UserNftWardrobeMessageComposer { Avatars = avatars }, ct)
            .ConfigureAwait(false);
    }
}
