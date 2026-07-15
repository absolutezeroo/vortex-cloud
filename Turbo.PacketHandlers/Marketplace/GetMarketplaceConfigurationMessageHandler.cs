using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Marketplace.Providers;
using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Messages.Incoming.Marketplace;
using Turbo.Primitives.Messages.Outgoing.Marketplace;

namespace Turbo.PacketHandlers.Marketplace;

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
