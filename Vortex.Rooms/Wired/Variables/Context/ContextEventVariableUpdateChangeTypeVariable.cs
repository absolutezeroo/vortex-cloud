using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>Which kind of change it was, as WiredVariableChangeKind numbers it.</summary>
public sealed class ContextEventVariableUpdateChangeTypeVariable(RoomGrain roomGrain)
    : ContextEventVariable(roomGrain)
{
    protected override string VariableName => WiredEventReadings.VariableChangeType;
    protected override ushort Order => 53;
}
