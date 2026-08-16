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
/// Which whole avatar the player is wearing, asked as the editor opens.
/// </summary>
/// <remarks>
/// <b>Wearing nothing is answered with silence, and that is deliberate.</b> The client reads this
/// into a field it later tests against null to decide whether an avatar is on, and a string off a
/// packet is never null — so an answer carrying empty strings tells it the opposite of what it means.
/// It would then open the editor on the outfit's fallback look instead of the player's own, and with
/// an empty fallback that path loads nothing at all: the editor would come up blank.
/// </remarks>
public class GetSelectedNftWardrobeOutfitMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetSelectedNftWardrobeOutfitMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetSelectedNftWardrobeOutfitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        NftOutfitSnapshot? worn = await _grainFactory
            .GetPlayerNftWardrobeGrain(new PlayerId(ctx.PlayerId))
            .GetWornAsync(ct)
            .ConfigureAwait(false);

        if (worn is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new UserNftWardrobeSelectionMessageComposer
                {
                    TokenId = worn.TokenId,
                    FallbackFigure = worn.FallbackFigure,
                    FallbackGender = worn.FallbackGender,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
