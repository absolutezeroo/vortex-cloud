using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Which wallets the player has linked.
///
/// Every player gets a Stardust wallet, derived deterministically from the player id — there is no
/// wallet grain and nowhere in the client to link an external one, but the address has to be
/// non-empty and stable across sessions, because the client keys its collections and reward claims
/// on it.
///
/// Returning nothing here is what used to hang two tabs. The client pushes the Stardust address
/// into its wallet list only when it is not empty, and both <c>CollectionsTab</c> and
/// <c>RewardClaimsTab</c> only ever leave their loading state from a message that is itself only
/// requested per wallet. With an empty list neither request is sent, so neither tab ever becomes
/// ready — the Flash client behaves the same way, so this is a server gap, not a client bug.
/// </summary>
public class GetCollectibleWalletAddressesMessageHandler
    : IMessageHandler<GetCollectibleWalletAddressesMessage>
{
    /// <summary>
    /// Habbo's own Stardust addresses are 0x-prefixed 40-hex-digit strings. The client never parses
    /// or validates the address — it uses it as an opaque key and shows the Stardust one under a
    /// friendly name — so a deterministic value in the real shape is enough.
    /// </summary>
    private static string StardustAddressFor(int playerId) =>
        "0x" + playerId.ToString("x", CultureInfo.InvariantCulture).PadLeft(40, '0');

    public async ValueTask HandleAsync(
        GetCollectibleWalletAddressesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new CollectibleWalletAddressesMessageComposer
                {
                    StardustWalletAddress = StardustAddressFor(ctx.PlayerId),
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
