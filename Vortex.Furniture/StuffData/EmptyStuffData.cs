using System.Text.Json.Serialization;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Furniture.StuffData;

namespace Vortex.Furniture.StuffData;

internal sealed class EmptyStuffData : StuffDataBase, IEmptyStuffData
{
    [JsonIgnore]
    public override StuffDataType StuffType => StuffDataType.EmptyKey;

    public override string GetLegacyString() => string.Empty;

    protected override StuffDataSnapshot BuildSnapshot() =>
        new EmptyStuffSnapshot()
        {
            StuffBitmask = GetBitmask(),
            UniqueNumber = UniqueNumber,
            UniqueSeries = UniqueSeries,
        };
}
