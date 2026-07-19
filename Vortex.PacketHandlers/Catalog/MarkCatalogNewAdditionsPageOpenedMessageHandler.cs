using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;

namespace Vortex.PacketHandlers.Catalog;

/// <summary>
/// Legitimate no-op: the client only ever sends this after GetCatalogIndexMessageHandler told it
/// CatalogIndexMessageComposer.NewAdditionsAvailable = true, and nothing currently sets that flag
/// to true (no "new additions" tracking exists -- it's always false). There is nothing to persist
/// yet; this stays a no-op until that tracking is built, rather than fabricating state for it.
/// </summary>
public class MarkCatalogNewAdditionsPageOpenedMessageHandler
    : IMessageHandler<MarkCatalogNewAdditionsPageOpenedMessage>
{
    public async ValueTask HandleAsync(
        MarkCatalogNewAdditionsPageOpenedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
