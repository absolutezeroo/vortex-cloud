using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>
/// A reading about the chain being executed rather than about the room: what the selectors picked,
/// what the signal carried.
/// </summary>
/// <remarks>
/// These have a value only WHILE a chain runs, which is why they read
/// <c>RoomWiredSystem.CurrentContext</c> rather than the room's state. Outside a run — the variable
/// picker listing them, a snapshot built for the client — there is no context and
/// <see cref="TryGetValue"/> answers false, which is the honest answer and the one every caller
/// already handles.
/// <para>
/// This used to return true with the value line commented out, so the whole tab read zero forever.
/// </para>
/// </remarks>
public abstract class ContextVariable(RoomGrain roomGrain) : WiredInternalVariable(roomGrain)
{
    protected override WiredVariableTargetType TargetType => WiredVariableTargetType.Context;

    public override bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        if (!CanBind(key))
        {
            return false;
        }

        IWiredContext? context = _roomGrain.WiredSystem.CurrentContext;

        if (context is null)
        {
            return false;
        }

        value = GetValueForContext(context);

        return true;
    }

    /// <summary>The one reading this variable takes of the running chain.</summary>
    protected abstract WiredVariableValue GetValueForContext(IWiredContext context);
}
