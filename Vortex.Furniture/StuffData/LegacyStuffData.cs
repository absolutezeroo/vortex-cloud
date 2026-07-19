using System.Text.Json.Serialization;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Furniture.StuffData;

namespace Vortex.Furniture.StuffData;

internal sealed class LegacyStuffData : StuffDataBase, ILegacyStuffData
{
    [JsonIgnore]
    public override StuffDataType StuffType => StuffDataType.LegacyKey;

    public string Data { get; set; } = DEFAULT_STATE;

    public override string GetLegacyString() => Data;

    public override void SetState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            state = DEFAULT_STATE;
        }

        Data = state;

        MarkDirty();
    }

    protected override StuffDataSnapshot BuildSnapshot() =>
        new LegacyStuffSnapshot()
        {
            StuffBitmask = GetBitmask(),
            UniqueNumber = UniqueNumber,
            UniqueSeries = UniqueSeries,
            Data = GetLegacyString(),
        };
}
