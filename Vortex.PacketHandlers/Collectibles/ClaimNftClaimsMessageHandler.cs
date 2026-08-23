using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Taking the Relics waiting in the Collectors Guild.
/// </summary>
/// <remarks>
/// The client's button claims <em>everything</em>: it sends the message with no arguments at all, so
/// both the claim id and the wallet arrive empty. There is no per-reward claim in this interface,
/// which is why this hands over the whole outstanding list rather than looking one up.
/// </remarks>
public class ClaimNftClaimsMessageHandler(
    IGrainFactory grainFactory,
    ILogger<ClaimNftClaimsMessageHandler> logger
) : IMessageHandler<ClaimNftClaimsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<ClaimNftClaimsMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        ClaimNftClaimsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int granted = await _grainFactory
            .GetPlayerNftClaimsGrain(new PlayerId(ctx.PlayerId))
            .ClaimAllAsync(ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Player {PlayerId} claimed {Granted} collectible reward(s) from the Rewards tab.",
            ctx.PlayerId,
            granted
        );

        // Nothing to take is reported as a failure rather than a silent success: the client empties
        // its list on success, so claiming zero and calling it a win would clear rewards that are
        // still owed — the "already claimed twice" case this button invites.
        await ctx.SendComposerAsync(
                new NftClaimResultMessageComposer
                {
                    Status = granted > 0 ? NftClaimStatus.Succeeded : NftClaimStatus.Failed,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
