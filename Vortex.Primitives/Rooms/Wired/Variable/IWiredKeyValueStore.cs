using System.Threading.Tasks;

namespace Vortex.Primitives.Rooms.Wired.Variable;

/// <summary>
/// A bag of wired variable values, keyed by <see cref="WiredVariableKey"/>.
/// <para>
/// The concrete store is a room-side type and must not cross into Primitives, so this is what the
/// room hands to wired furniture instead. Furniture that keeps a store of its own (a persistent
/// variable box holding its values in its own ExtraData) and furniture reading the room's shared
/// store therefore talk to the same five operations.
/// </para>
/// </summary>
public interface IWiredKeyValueStore
{
    bool ContainsKey(WiredVariableKey key);

    bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value);

    /// <summary>Writes a value that is not there yet, or overwrites when
    /// <paramref name="replace"/> is set. False when the key existed and replace was not asked for.</summary>
    Task<bool> GiveValueAsync(WiredVariableKey key, WiredVariableValue value, bool replace = false);

    /// <summary>Updates a value that already exists. False when the key is absent.</summary>
    Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    );

    bool RemoveValue(WiredVariableKey key);
}
