using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record SellablePetPalettesMessageComposer : IComposer
{
    [Id(0)]
    public required string ProductCode { get; init; }

    [Id(1)]
    public required ImmutableArray<PetPaletteEntry> Palettes { get; init; }
}
