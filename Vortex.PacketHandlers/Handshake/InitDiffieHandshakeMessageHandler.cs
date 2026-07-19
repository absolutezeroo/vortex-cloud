using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Crypto;
using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Messages.Outgoing.Handshake;

namespace Vortex.PacketHandlers.Handshake;

public class InitDiffieHandshakeMessageHandler(IDiffieService diffieService)
    : IMessageHandler<InitDiffieHandshakeMessage>
{
    private readonly IDiffieService _diffieService = diffieService;

    public async ValueTask HandleAsync(
        InitDiffieHandshakeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        string prime = _diffieService.GetSignedPrime();
        string generator = _diffieService.GetSignedGenerator();

        await ctx.SendComposerAsync(
                new InitDiffieHandshakeMessageComposer { Prime = prime, Generator = generator },
                ct
            )
            .ConfigureAwait(false);
    }
}
