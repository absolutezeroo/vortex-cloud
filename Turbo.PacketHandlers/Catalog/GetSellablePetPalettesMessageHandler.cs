using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Pets.Providers;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.PacketHandlers.Catalog;

public class GetSellablePetPalettesMessageHandler(IPetPaletteProvider petPaletteProvider)
    : IMessageHandler<GetSellablePetPalettesMessage>
{
    private readonly IPetPaletteProvider _petPaletteProvider = petPaletteProvider;

    public async ValueTask HandleAsync(
        GetSellablePetPalettesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        string productCode = message.LocalizationId.ToString();
        ImmutableArray<PetPaletteEntry> palettes = _petPaletteProvider
            .GetPalettesForType(message.LocalizationId)
            .ToImmutableArray();

        await ctx.SendComposerAsync(
                new SellablePetPalettesMessageComposer
                {
                    ProductCode = productCode,
                    Palettes = palettes,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
