using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Primitives.Rooms.Wired.Variable;

public interface IWiredVariable : IWiredVariableStore
{
    public bool CanBind(in WiredVariableKey key);
    public WiredVariableSnapshot GetVarSnapshot();
}
