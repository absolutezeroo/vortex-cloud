using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>How many players the signal that started this run carried.</summary>
public sealed class ContextSignalUserCountVariable(RoomGrain roomGrain) : ContextVariable(roomGrain)
{
    protected override string VariableName => "@signal_user_count";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 40;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForContext(IWiredContext context) =>
        WiredVariableValue.Parse(context.Signal.SelectedPlayerIds.Count);
}
