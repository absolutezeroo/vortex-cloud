using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>How many furni the signal that started this run carried.</summary>
public sealed class ContextSignalFurniCountVariable(RoomGrain roomGrain)
    : ContextVariable(roomGrain)
{
    protected override string VariableName => "@signal_furni_count";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 30;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForContext(IWiredContext context) =>
        WiredVariableValue.Parse(context.Signal.SelectedFurniIds.Count);
}
