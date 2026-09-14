using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>The value it holds after it.</summary>
public sealed class ContextEventVariableUpdateNewValueVariable(RoomGrain roomGrain)
    : ContextEventVariable(roomGrain)
{
    protected override string VariableName => WiredEventReadings.VariableNewValue;
    protected override ushort Order => 51;
}
