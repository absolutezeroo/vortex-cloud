using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>The value the variable held before the change.</summary>
public sealed class ContextEventVariableUpdateOldValueVariable(RoomGrain roomGrain)
    : ContextEventVariable(roomGrain)
{
    protected override string VariableName => WiredEventReadings.VariableOldValue;
    protected override ushort Order => 50;
}
