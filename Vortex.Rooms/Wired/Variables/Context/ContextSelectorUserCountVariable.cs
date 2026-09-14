using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

/// <summary>How many players the selectors handed to this run.</summary>
public sealed class ContextSelectorUserCountVariable(RoomGrain roomGrain)
    : ContextVariable(roomGrain)
{
    protected override string VariableName => "@selector_user_count";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 20;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForContext(IWiredContext context) =>
        WiredVariableValue.Parse(context.Selected.SelectedPlayerIds.Count);
}
