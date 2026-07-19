using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.PacketHandlers.Catalog;

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
        int petType = ExtractPetTypeFromLocalizationId(message.LocalizationId);

        ImmutableArray<PetPaletteEntry> palettes =
            petType >= 0 ? _petPaletteProvider.GetPalettesForType(petType).ToImmutableArray() : [];

        await ctx.SendComposerAsync(
                new SellablePetPalettesMessageComposer
                {
                    ProductCode = message.LocalizationId,
                    Palettes = palettes,
                },
                ct
            )
            .ConfigureAwait(false);
    }

    // Mirror of Flash client getPetTypeIndexFromProduct: scan from the end for trailing digits,
    // then return the integer formed by those digits if they are preceded by a non-digit char.
    private static int ExtractPetTypeFromLocalizationId(string localizationId)
    {
        if (string.IsNullOrEmpty(localizationId))
        {
            return -1;
        }

        int i = localizationId.Length - 1;

        while (i >= 0 && char.IsDigit(localizationId[i]))
        {
            i--;
        }

        if (i > 0 && i < localizationId.Length - 1)
        {
            return int.Parse(localizationId.AsSpan(i + 1));
        }

        return -1;
    }
}
