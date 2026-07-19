using System;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Snapshots.StuffData;

namespace Vortex.Primitives.Furniture.StuffData;

public interface IStuffData
{
    public StuffDataType StuffType { get; }
    public int UniqueNumber { get; }
    public int UniqueSeries { get; }
    public int GetBitmask();
    public bool IsUnique();
    public int GetState();
    public void SetState(string state);
    public string GetLegacyString();
    public void SetAction(Func<Task>? onSnapshotChanged);
    public void MarkDirty();
    public StuffDataSnapshot GetSnapshot();
}
