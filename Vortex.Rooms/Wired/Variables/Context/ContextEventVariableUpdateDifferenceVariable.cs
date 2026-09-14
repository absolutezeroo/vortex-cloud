using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>New minus old, so a chain can react to how much it moved.</summary>
public sealed class ContextEventVariableUpdateDifferenceVariable(RoomGrain roomGrain)
    : ContextEventVariable(roomGrain)
{
    protected override string VariableName => WiredEventReadings.VariableDifference;
    protected override ushort Order => 52;
}
