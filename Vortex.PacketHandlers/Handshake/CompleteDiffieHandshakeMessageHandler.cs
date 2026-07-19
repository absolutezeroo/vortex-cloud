using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Vortex.Crypto.Configuration;
using Vortex.Messages.Registry;
using Vortex.Primitives.Crypto;
using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Messages.Outgoing.Handshake;

namespace Vortex.PacketHandlers.Handshake;

public class CompleteDiffieHandshakeMessageHandler(
    IDiffieService diffieService,
    IOptions<CryptoConfig> config
) : IMessageHandler<CompleteDiffieHandshakeMessage>
{
    private readonly IDiffieService _diffieService = diffieService;
    private readonly CryptoConfig _config = config.Value;

    public async ValueTask HandleAsync(
        CompleteDiffieHandshakeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        byte[] sharedKey = _diffieService.GetSharedKey(message.SharedKey);

        await ctx.SendComposerAsync(
                new CompleteDiffieHandshakeMessageComposer
                {
                    PublicKey = _diffieService.GetPublicKey(),
                    ServerClientEncryption = _config.EnableServerToClientEncryption,
                },
                ct
            )
            .ConfigureAwait(false);

        ctx.SetupEncryption(sharedKey, _config.EnableServerToClientEncryption);
    }
}
