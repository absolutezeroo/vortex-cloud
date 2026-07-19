using System.Text.Json.Serialization;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Furniture.StuffData;

namespace Vortex.Furniture.StuffData;

internal sealed class VoteStuffData : StuffDataBase, IVoteStuffData
{
    [JsonIgnore]
    public override StuffDataType StuffType => StuffDataType.VoteKey;

    public string Data { get; set; } = DEFAULT_STATE;
    public int Result { get; set; } = 0;

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

    public int GetResult() => Result;

    public void SetResult(int result)
    {
        Result = result;

        MarkDirty();
    }

    protected override StuffDataSnapshot BuildSnapshot() =>
        new VoteStuffSnapshot()
        {
            StuffBitmask = GetBitmask(),
            UniqueNumber = UniqueNumber,
            UniqueSeries = UniqueSeries,
            Data = GetLegacyString(),
            Result = GetResult(),
        };
}
