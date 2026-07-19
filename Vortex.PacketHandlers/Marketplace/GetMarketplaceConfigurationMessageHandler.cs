using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Marketplace.Providers;
using Vortex.Primitives.Marketplace.Snapshots;
using Vortex.Primitives.Messages.Incoming.Marketplace;
using Vortex.Primitives.Messages.Outgoing.Marketplace;

namespace Vortex.PacketHandlers.Marketplace;

public class GetMarketplaceConfigurationMessageHandler(
    IMarketplaceSettingsProvider settingsProvider
) : IMessageHandler<GetMarketplaceConfigurationMessage>
{
    private readonly IMarketplaceSettingsProvider _settingsProvider = settingsProvider;

    public async ValueTask HandleAsync(
        GetMarketplaceConfigurationMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        MarketplaceSettingsSnapshot settings = _settingsProvider.GetSettings();

        await ctx.SendComposerAsync(
                new MarketplaceConfigurationEventMessageComposer
                {
                    Enabled = true,
                    Commission = settings.CommissionPercent,
                    Credits = 0,
                    Advertisements = 0,
                    MinimumPrice = 1,
                    MaximumPrice = 9999999,
                    OfferTime = settings.OfferDurationSeconds,
                    DisplayTime = settings.OfferDurationSeconds,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
