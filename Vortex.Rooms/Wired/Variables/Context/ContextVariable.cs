using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Context;

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

        //value = GetValueForRoom(_roomGrain);

        return true;
    }
}
