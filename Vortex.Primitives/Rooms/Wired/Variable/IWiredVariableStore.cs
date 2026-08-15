using System.Threading.Tasks;

namespace Vortex.Primitives.Rooms.Wired.Variable;

public interface IWiredVariableStore
{
    public bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value);
    public Task<bool> GiveValueAsync(
        WiredVariableKey key,
        WiredVariableValue value,
        bool replace = false
    );
    public Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    );
    public bool RemoveValue(WiredVariableKey key);

    /// <summary>When this key was first written and last written, in Unix milliseconds; false when
    /// that is not known. See <see cref="IWiredKeyValueStore.TryGetTimestamps"/>.</summary>
    public bool TryGetTimestamps(
        in WiredVariableKey key,
        out long createdAtMs,
        out long updatedAtMs
    );
}
