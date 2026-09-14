using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>
/// A reading of the event that started the chain — <c>@event.…</c> in the picker's context tab.
/// </summary>
/// <remarks>
/// The values are put into the context by <see cref="WiredEventReadings"/> when the execution
/// context is built, keyed by the variable's own name. A chain started by a different kind of event
/// simply has no entry, and the reading answers false rather than zero — which is the difference
/// between "this chain was not about a variable change" and "the value was zero".
/// </remarks>
public abstract class ContextEventVariable(RoomGrain roomGrain) : ContextVariable(roomGrain)
{
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;

    protected override WiredVariableFlags Flags => WiredVariableFlags.HasValue;

    protected override WiredVariableValue GetValueForContext(IWiredContext context) =>
        context.Variables.TryGetValue(VariableName, out int value)
            ? WiredVariableValue.Parse(value)
            : WiredVariableValue.Default;
}
