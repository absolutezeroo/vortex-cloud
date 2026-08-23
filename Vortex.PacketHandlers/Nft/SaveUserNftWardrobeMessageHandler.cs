using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Nft;
using Vortex.Protocol.Messages.Outgoing.Nft;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Nft;

/// <summary>
/// Putting on one of the avatars the player owns.
/// </summary>
/// <remarks>
/// This is the editor's save button when an avatar was picked; picking an ordinary look sends the
/// usual figure update instead, which is what takes the costume back off. The reply is the same
/// selection message the editor asks for on open, so the client learns the fallback look it will
/// need to get out again — and it is only sent on success, for the null reason described there.
/// </remarks>
public class SaveUserNftWardrobeMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SaveUserNftWardrobeMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SaveUserNftWardrobeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || !int.TryParse(
                message.CopyId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int copyId
            )
        )
        {
            return;
        }

        NftOutfitSnapshot? worn = await _grainFactory
            .GetPlayerNftWardrobeGrain(new PlayerId(ctx.PlayerId))
            .WearAsync(copyId, ct)
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
