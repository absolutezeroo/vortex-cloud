using Orleans;

namespace Vortex.Primitives.Snapshots.Catalog;

[GenerateSerializer, Immutable]
public sealed record GiftWrappingConfigurationSnapshot(
    bool IsWrappingEnabled,
    int WrappingPrice,
    int[] StuffTypes,
    int[] BoxTypes,
    int[] RibbonTypes,
    int[] DefaultStuffTypes
);
