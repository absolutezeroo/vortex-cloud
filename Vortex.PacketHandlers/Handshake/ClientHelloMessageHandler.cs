using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Handshake;

namespace Vortex.PacketHandlers.Handshake;

public class ClientHelloMessageHandler() : IMessageHandler<ClientHelloMessage>
{
    public async ValueTask HandleAsync(
        ClientHelloMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (message.Production is null)
        {
            await ctx.CloseSessionAsync().ConfigureAwait(false);

            return;
        }

        // The client announces its own build here, and a revision declares the same string as its
        // id: Revision20260701.Revision is "WIN63-202607011411-782849652", exactly what this field
        // carries. Until now every session kept the manager's default revision for its whole life,
        // so a second registered revision could never be selected by the client that speaks it.
        //
        // A build nobody registered leaves the session on an id no revision answers to: incoming
        // packets then throw in PackageHandler and outgoing ones are dropped by PackageEncoder --
        // the session stays open but stops speaking. That is the upstream behaviour, kept as-is.
        ctx.SetRevisionId(message.Production);

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
