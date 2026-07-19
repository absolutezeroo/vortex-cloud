using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class GetCatalogIndexMessageHandler(
    ICatalogService catalogService,
    ILogger<GetCatalogIndexMessageHandler> logger
) : IMessageHandler<GetCatalogIndexMessage>
{
    private readonly ICatalogService _catalogService = catalogService;
    private readonly ILogger<GetCatalogIndexMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        GetCatalogIndexMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        try
        {
            CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(message.CatalogType);

            await ctx.SendComposerAsync(new CatalogIndexMessageComposer { Catalog = snapshot }, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to serve catalog index for type {CatalogType}",
                message.CatalogType
            );
        }
    }
}
