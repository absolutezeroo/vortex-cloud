using System;
using System.Text.Json.Serialization;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Furniture.StuffData;

namespace Vortex.Furniture.StuffData;

internal sealed class CrackableStuffData : StuffDataBase, ICrackableStuffData
{
    [JsonIgnore]
    public override StuffDataType StuffType => StuffDataType.CrackableKey;

    public string Data { get; set; } = DEFAULT_STATE;
    public int Hits { get; set; }
    public int Target { get; set; }

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

    public int AddHit()
    {
        Hits++;

        MarkDirty();

        return Hits;
    }

    public void SetTarget(int target)
    {
        int normalized = Math.Max(0, target);

        if (normalized == Target)
        {
            return;
        }

        Target = normalized;

        MarkDirty();
    }

    protected override StuffDataSnapshot BuildSnapshot() =>
        new CrackableStuffSnapshot
        {
            StuffBitmask = GetBitmask(),
            UniqueNumber = UniqueNumber,
            UniqueSeries = UniqueSeries,
            Data = GetLegacyString(),
            Hits = Hits,
            Target = Target,
        };
}
