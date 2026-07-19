using System.Collections.Generic;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredSelectionSet
{
    public HashSet<int> SelectedFurniIds { get; }
    public HashSet<int> SelectedPlayerIds { get; }

    public bool HasFurni { get; }
    public bool HasPlayers { get; }

    public IWiredSelectionSet UnionWith(IWiredSelectionSet other);
    public WiredSelectionSetSnapshot GetSnapshot();
}
